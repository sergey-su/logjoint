using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersListBox;

namespace LogJoint.UI
{
	public partial class FiltersListView : UserControl, IView
	{
		IPresenterEvents presenter;

		public FiltersListView()
		{
			InitializeComponent();
			InitCounterHeader();
		}

		void IView.SetPresenter(IPresenterEvents presenter)
		{
			this.presenter = presenter;
		}
		void IView.BeginUpdate() { list.BeginUpdate(); }
		void IView.EndUpdate() { list.EndUpdate(); }
		IViewItem IView.CreateItem(IFilter filter, string key) { return new Item(filter, key); }
		int IView.Count { get { return list.Items.Count; } }
		IViewItem IView.GetItem(int index) { return GetItem(index); }
		void IView.RemoveAt(int index) { list.Items.RemoveAt(index); }
		void IView.Remove(IViewItem item) { list.Items.Remove(GetItem(item)); }
		void IView.Insert(int index, IViewItem item) { list.Items.Insert(index, GetItem(item)); }
		int IView.GetItemIndexByKey(string key) { return list.Items.IndexOfKey(key); }
		IEnumerable<IViewItem> IView.SelectedItems
		{
			get
			{
				foreach (ListViewItem i in list.SelectedItems)
					if (GetItem(i) != null)
						yield return GetItem(i);
			}
		}
		void IView.SetEnabled(bool value) { list.Enabled = value; }




		Item GetItem(IViewItem intf)
		{
			return intf as Item;
		}

		Item GetItem(ListViewItem i)
		{
			return i as Item;
		}

		Item GetItem(int i)
		{
			return GetItem(list.Items[i]);
		}

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			itemColumnHeader.Width = list.ClientSize.Width - counterColumnHeader.Width - 15;
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			presenter.OnItemChecked(GetItem(e.Item));
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			ContextMenuItem enabledItems;
			ContextMenuItem checkedItems;
			presenter.OnContextMenuOpening(out enabledItems, out checkedItems);

			filterEnabledToolStripMenuItem.Enabled = (enabledItems & ContextMenuItem.FilterEnabled) != 0;
			propertiesToolStripMenuItem.Enabled = (enabledItems & ContextMenuItem.Properties) != 0;

			filterEnabledToolStripMenuItem.Checked = (checkedItems & ContextMenuItem.FilterEnabled) != 0;
		}

		private void filterEnabledToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnFilterEnabledMenuItemClicked();
		}

		private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnPropertiesMenuItemClicked();
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (GetItem(e.Index) == null)
				e.NewValue = CheckState.Checked;
		}

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			presenter.OnSelectionChanged();
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				presenter.OnENTERPressed();
		}

		private void InitCounterHeader()
		{
			using (Graphics g = this.CreateGraphics())
			{
				int maxCounter = 999999;
				int maxWidth = 0;
				for (FilterAction a = FilterAction.Include; a <= FilterAction.Exclude; ++a)
					maxWidth = Math.Max(maxWidth, (int)g.MeasureString(Presenter.GetFilterCounterString(true, a, maxCounter, true), this.Font).Width);
				counterColumnHeader.Width = maxWidth + 5;
			}
		}

		class Item : ListViewItem, IViewItem
		{
			public Item(IFilter filter, string key)
			{
				this.filter = filter;
				Name = key;
				SubItems.Add("");
				if (filter == null)
					this.ForeColor = SystemColors.GrayText;
			}

			IFilter IViewItem.Filter { get { return filter; } }
			string IViewItem.Text { get { return base.Text; } set { base.Text = value; } }
			void IViewItem.SetImageType(ViewItemImageType value)
			{
				if (value == ViewItemImageType.Exclude)
					ImageIndex = 0;
				else if (value == ViewItemImageType.Include)
					ImageIndex = 2;
				else
					ImageIndex = 1;
			}
			void IViewItem.SetSubText(string text) { SubItems[1].Text = text; }
			bool IViewItem.Checked { get { return base.Checked; } set { base.Checked = value; } }

			readonly IFilter filter;
		};
	}

}
