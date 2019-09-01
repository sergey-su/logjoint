using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Windows.Reactive
{
	class ListBoxController<Item> : IListBoxController<Item> where Item : class, IListItem
	{
		readonly ListBox listBox;
		IReadOnlyList<Item> currentList = new Item[0];
		bool updating;

		public ListBoxController(ListBox listBox)
		{
			this.listBox = listBox;
			listBox.SelectedIndexChanged += (s, e) =>
			{
				if (updating)
					return;
				OnSelect?.Invoke(listBox.SelectedItems.OfType<ViewItem>().Select(i => i.item).ToArray());
			};
		}

		public Action<Item[]> OnSelect { get; set; }
		public Action<Item, int, Item> OnUpdateRow { get; set; }
		bool IListBoxController<Item>.IsUpdating => updating;

		void IListBoxController<Item>.Update(IReadOnlyList<Item> newList)
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
							var newViewItem = new ViewItem { item = (Item)e.Item };
							listBox.Items.Insert(e.Index, newViewItem);
							OnUpdateRow?.Invoke(newViewItem.item, e.Index, null);
							break;
						case ListEdit.EditType.Delete:
							BeginUpdate();
							listBox.Items.RemoveAt(e.Index);
							break;
						case ListEdit.EditType.Reuse:
							if (OnUpdateRow != null)
							{
								var existingViewItem = (ViewItem)listBox.Items[e.Index];
								var oldItem = existingViewItem.item;
								existingViewItem.item = (Item)e.Item;
								OnUpdateRow?.Invoke(existingViewItem.item, e.Index, oldItem);
							}
							else
							{
								listBox.Items[e.Index] = new ViewItem { item = (Item)e.Item };
							}
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

		Item IListBoxController<Item>.Map(object listBoxItem)
		{
			return (listBoxItem as ViewItem)?.item;
		}

		class ViewItem
		{
			public Item item;

			public override string ToString() => item.ToString();
		};
	}
}
