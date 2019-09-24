using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Reactive
{
	/// <summary>
	/// Represents an single edit action that should be performed on UI list.
	/// The actions are imperative commands. The diff between two immutable versions
	/// of the list (represented as IListItem collection) can be converted to a series of actions
	/// that can be performed to transform mutable UI tree from one version to another.
	/// </summary>
	public struct ListEdit
	{
		public enum EditType
		{
			Insert,
			Delete,
			Reuse,
			Select,
			Deselect
		};
		public EditType Type { get; internal set; }
		public IListItem Item { get; internal set; }
		public IListItem OldItem { get; internal set; }
		public int Index { get; internal set; }

		public override string ToString()
		{
			if (Type == EditType.Select || Type == EditType.Deselect)
				return $"({Item}).{Type}";
			else
				return $"({Item}).{Type} ({OldItem})->({Item}) at {Index}";
		}

		public static List<ListEdit> GetListEdits(
			IReadOnlyList<IListItem> list1,
			IReadOnlyList<IListItem> list2
		)
		{
			var result = new List<ListEdit>();

			var edits = EditDistance.GetEditDistance(list1, list2, (c1, c2) =>
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
					var i1 = list1[i.Value];
					var i2 = list2[j.Value];
					if (i1 != i2)
					{
						result.Add(new ListEdit
						{
							Type = EditType.Reuse,
							OldItem = i1,
							Item = i2,
							Index = targetIdx
						});
					}
					if (i2.IsSelected != i1.IsSelected)
					{
						result.Add(new ListEdit
						{
							Type = i2.IsSelected ? EditType.Select : EditType.Deselect,
							Item = i2,
							Index = targetIdx
						});
					}
					++targetIdx;
				}
				else if (i.HasValue)
				{
					result.Add(new ListEdit
					{
						Type = EditType.Delete,
						OldItem = list1[i.Value],
						Index = targetIdx
					});
				}
				else
				{
					var i2 = list2[j.Value];
					result.Add(new ListEdit
					{
						Type = EditType.Insert,
						Item = i2,
						Index = targetIdx
					});
					if (i2.IsSelected)
					{
						result.Add(new ListEdit
						{
							Type = EditType.Select,
							Item = i2,
							Index = targetIdx
						});
					}
					++targetIdx;
				}
			}

			return result;
		}
	};
}
