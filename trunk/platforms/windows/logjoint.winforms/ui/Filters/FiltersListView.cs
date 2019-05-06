using System;
using System.Collections.Generic;
using System.ComponentModel;
using LogJoint.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersListBox;

namespace LogJoint.UI
{
	public partial class FiltersListView : UserControl, IView
	{
		IViewEvents presenter;
		bool ignoreNextCheck;

		public FiltersListView()
		{
			InitializeComponent();
		}

		void IView.SetPresenter(IViewEvents presenter)
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
			itemColumnHeader.Width = list.ClientSize.Width - 15;
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			presenter.OnItemChecked(GetItem(e.Item));
		}

		private void list_MouseDown(object sender, MouseEventArgs e)
		{
			if ((e.Button == MouseButtons.Left) && (e.Clicks > 1))
			{
				ignoreNextCheck = true;
				presenter.OnDoubleClicked();
			}
		}

		private void list_MouseMove(object sender, MouseEventArgs e)
		{
			var ht = list.HitTest(e.Location);
			var item = GetItem(ht?.Item);
			if (item != null)
			{
				if (ht.Location == ListViewHitTestLocations.StateImage)
					item.ToolTipText = item.CheckboxTooltip;
				else if (ht.Location == ListViewHitTestLocations.Image)
					item.ToolTipText = item.ActionTooltip;
				else
					item.ToolTipText = "";
			}
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
			if (ignoreNextCheck)
			{
				e.NewValue = e.CurrentValue;
				ignoreNextCheck = false;
			}
			else if (GetItem(e.Index)?.IsCheckable != true)
			{
				e.NewValue = CheckState.Checked;
			}
		}

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			presenter.OnSelectionChanged();
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				presenter.OnEnterPressed();
			else if (e.KeyCode == Keys.Delete)
				presenter.OnDeletePressed();
		}

		class Item : ListViewItem, IViewItem
		{
			public bool IsCheckable = true;
			Color? color;

			public Item(IFilter filter, string key)
			{
				this.filter = filter;
				Name = key;
				if (filter == null)
					this.ForeColor = System.Drawing.SystemColors.GrayText;
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
			bool? IViewItem.Checked
			{
				get { return base.Checked; }
				set { base.Checked = value.GetValueOrDefault(true); IsCheckable = value != null; }
			}

			Color? IViewItem.Color
			{
				get { return color; }
				set
				{
					color = value;
					BackColor = color != null ? color.Value.ToColor() : System.Drawing.Color.Empty;
				}
			}

			public string CheckboxTooltip { get; set; }

			public string ActionTooltip { get; set; }

			readonly IFilter filter;
		};
	}
}
