using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Reactive
{
	/// <summary>
	/// Represents an signle edit action that should be performed on UI tree.
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
		public EditType Type;
		public ITreeNode Node;
		public ITreeNode OldChild;
		public ITreeNode NewChild;
		public int ChildIndex;

		public override string ToString()
		{
			if (Type == EditType.Expand || Type == EditType.Collapse || Type == EditType.Select)
				return $"({Node}).{Type}";
			else
				return $"({Node}).{Type} ({OldChild})->({NewChild}) at {ChildIndex}";
		}

		// todo: add tests to win ptoject
		public static List<TreeEdit> GetTreeEdits(ITreeNode root1, ITreeNode root2)
		{
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
				if (n.IsExpanded)
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
			}

			void GetEdits(ITreeNode n1, ITreeNode n2)
			{
				var edits = EditDistance.GetEditDistance(n1.Children, n2.Children, (c1, c2) =>
				{
					return
						c1 == null || c2 == null ? 1 : // let deletion/insertion of a node to cost 1
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
						result.Add(new TreeEdit
						{
							Type = EditType.Reuse,
							Node = n2,
							OldChild = c1,
							NewChild = c2,
							ChildIndex = targetIdx
						});
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
