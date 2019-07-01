using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Windows.Reactive
{
	class ListBoxController : IListBoxController
	{
		readonly ListBox listBox;
		IReadOnlyList<IListItem> currentList = new IListItem[0];
		bool updating;

		public ListBoxController(ListBox listBox)
		{
			this.listBox = listBox;
			listBox.SelectedIndexChanged += (s, e) =>
			{
				if (updating)
					return;
				OnSelect?.Invoke(listBox.SelectedItems.OfType<IListItem>().ToArray());
			};
		}

		public Action<IListItem[]> OnSelect { get; set; }
		public Action<IListItem, int, IListItem> OnUpdateRow { get; set; }
		bool IListBoxController.IsUpdating => updating;

		void IListBoxController.Update(IReadOnlyList<IListItem> newList)
		{
			bool updateBegun = false;
			void BeginUpdate()
			{
				if (!updateBegun)
				{
					// Call ListBox's BeginUpdate/EndUpdate only when needed
					// tree structure changed to avoid flickering when only selection changes.
					listBox.BeginUpdate();
					updateBegun = true;
				}
			}

			updating = true;
			try
			{
				var edits = ListEdit.GetListEdits(currentList, newList);
				currentList = newList;

				foreach (var e in edits)
				{
					switch (e.Type)
					{
						case ListEdit.EditType.Insert:
							BeginUpdate();
							listBox.Items.Insert(e.Index, e.Item);
							OnUpdateRow?.Invoke(e.Item, e.Index, null);
							break;
						case ListEdit.EditType.Delete:
							BeginUpdate();
							listBox.Items.RemoveAt(e.Index);
							break;
						case ListEdit.EditType.Reuse:
							listBox.Items[e.Index] = e.Item;
							OnUpdateRow?.Invoke(e.Item, e.Index, e.OldItem);
							break;
						case ListEdit.EditType.Select:
							listBox.SelectedIndices.Add(e.Index);
							break;
						case ListEdit.EditType.Deselect:
							listBox.SelectedIndices.Remove(e.Index);
							break;
					}
				}
			}
			finally
			{
				if (updateBegun)
				{
					listBox.EndUpdate();
				}
				updating = false;
			}
		}
	}
}
