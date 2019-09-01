using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Reactive
{
	/// <summary>
	/// Represents an single edit action that should be performed on UI tree.
	/// The actions are imperative commands. The diff between two immutable versions
	/// of the tree (represented as ITreeNode) can be converted to a series of actions
	/// that can be performed to transform mutable UI tree from one version to another.
	/// </summary>
	public struct TreeEdit
	{
		public enum EditType
		{
			Insert,
			Delete,
			Reuse,
			Expand,
			Collapse,
			Select,
			Deselect
		};
		public EditType Type { get; internal set; }
		public ITreeNode Node { get; internal set; }
		public ITreeNode OldChild { get; internal set; }
		public ITreeNode NewChild { get; internal set; }
		public int ChildIndex { get; internal set; }

		public class Options
		{
			/// <summary>
			/// Generate sequence of edits that ensures a node is expanded
			/// when its children are inserted and initialized.
			/// Useful for UIs that do not support modification of collased nodes.
			/// </summary>
			public bool TemporariltyExpandParentToInitChildren { get; set; }

			internal readonly static Options @default = new Options();
		};

		public override string ToString()
		{
			if (Type == EditType.Expand || Type == EditType.Collapse || Type == EditType.Select || Type == EditType.Deselect)
				return $"({Node}).{Type}";
			else
				return $"({Node}).{Type} ({OldChild})->({NewChild}) at {ChildIndex}";
		}

		public static List<TreeEdit> GetTreeEdits(ITreeNode root1, ITreeNode root2, Options options = null)
		{
			options = options ?? Options.@default;

			var result = new List<TreeEdit>();

			void AddNewNodeEdits(ITreeNode n, ITreeNode parent, int nodeIndex)
			{
				result.Add(new TreeEdit
				{
					Type = EditType.Insert,
					Node = parent,
					NewChild = n,
					ChildIndex = nodeIndex
				});
				if (n.IsExpanded || options.TemporariltyExpandParentToInitChildren)
				{
					result.Add(new TreeEdit
					{
						Type = EditType.Expand,
						Node = n
					});
				}
				if (n.IsSelected)
				{
					result.Add(new TreeEdit
					{
						Type = EditType.Select,
						Node = n
					});
				}
				int childIndex = 0;
				foreach (var c in n.Children)
				{
					AddNewNodeEdits(c, n, childIndex++);
				}
				if (options.TemporariltyExpandParentToInitChildren && !n.IsExpanded)
				{
					result.Add(new TreeEdit
					{
						Type = EditType.Collapse,
						Node = n
					});
				}
			}

			void GetEdits(ITreeNode n1, ITreeNode n2)
			{
				if (n1 == n2)
					return;
				var edits = EditDistance.GetEditDistance(n1.Children, n2.Children, (c1, c2) =>
				{
					return
						c1 == null || c2 == null ? 1 : // let deletion/insertion of a node cost 1
						c1.Key == c2.Key ? 0 : // encourage reuse of nodes with same Key by giving it no cost,
						1; // reuse nodes with different Keys
				}).edits;
				int targetIdx = 0;
				foreach (var (i, j, _) in edits)
				{
					if (i.HasValue && j.HasValue)
					{
						var c1 = n1.Children[i.Value];
						var c2 = n2.Children[j.Value];
						if (c1 != c2)
						{
							result.Add(new TreeEdit
							{
								Type = EditType.Reuse,
								Node = n2,
								OldChild = c1,
								NewChild = c2,
								ChildIndex = targetIdx
							});
						}
						++targetIdx;
						GetEdits(c1, c2);
						if (c2.IsExpanded != c1.IsExpanded)
						{
							result.Add(new TreeEdit
							{
								Type = c2.IsExpanded ? EditType.Expand : EditType.Collapse,
								Node = c2
							});
						}
						if (c2.IsSelected != c1.IsSelected)
						{
							result.Add(new TreeEdit
							{
								Type = c2.IsSelected ? EditType.Select : EditType.Deselect,
								Node = c2
							});
						}
					}
					else if (i.HasValue)
					{
						result.Add(new TreeEdit
						{
							Type = EditType.Delete,
							Node = n2,
							OldChild = n1.Children[i.Value],
							ChildIndex = targetIdx
						});
					}
					else
					{
						AddNewNodeEdits(n2.Children[j.Value], n2, targetIdx);
						++targetIdx;
					}
				}
			}
			GetEdits(root1, root2);
			return result;
		}
	};
}
