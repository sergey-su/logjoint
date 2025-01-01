using System;
using System.Collections.Generic;
using System.ComponentModel;
using LogJoint.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FiltersListBox;
using LogJoint.Extensibility;
using System.Reflection;
using System.Linq;

namespace LogJoint.UI
{
    public partial class FiltersListView : UserControl
    {
        IViewModel viewModel;
        bool ignoreNextCheck;
        int updateLock;
        ISubscription subscription;

        public FiltersListView()
        {
            InitializeComponent();
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            var updateEnablement = Updaters.Create(() => viewModel.IsEnabled, value => { list.Enabled = value; });
            var updateList = Updaters.Create(() => viewModel.Items, UpdateItems);

            subscription = viewModel.ChangeNotification.CreateSubscription(() =>
            {
                updateEnablement();
                updateList();
            });
        }

        FilterListViewItem GetFilterListViewItem(ListViewItem i) => i as FilterListViewItem;

        FilterListViewItem GetFilterListViewItem(int i) => GetFilterListViewItem(list.Items[i]);

        void UpdateItems(IReadOnlyList<IViewItem> viewItems)
        {
            updateLock++;
            list.BeginUpdate();
            try
            {
                FilterListViewItem defActionItem = null;
                IViewItem defActionViewItem = null;
                for (int i = list.Items.Count - 1; i >= 0; --i)
                {
                    if (GetFilterListViewItem(i).ViewItem.IsDefaultActionItem)
                        defActionItem = GetFilterListViewItem(i);
                }
                int nextListViewItemIndex = 0;
                foreach (var viewItem in viewItems)
                {
                    if (viewItem.IsDefaultActionItem)
                    {
                        defActionViewItem = viewItem;
                        continue;
                    }
                    FilterListViewItem lvi;
                    int existingItemIdx = list.Items.IndexOfKey(viewItem.Key);
                    if (existingItemIdx < 0)
                    {
                        lvi = new FilterListViewItem(viewItem);
                        list.Items.Insert(nextListViewItemIndex, lvi);
                    }
                    else
                    {
                        lvi = GetFilterListViewItem(existingItemIdx);
                        if (existingItemIdx != nextListViewItemIndex)
                        {
                            list.Items.RemoveAt(existingItemIdx);
                            list.Items.Insert(nextListViewItemIndex, lvi);
                        }
                    }
                    ++nextListViewItemIndex;

                    lvi.Text = viewItem.ToString();
                    lvi.Color = viewItem.Color;
                    lvi.IsChecked = viewItem.IsChecked;
                    lvi.CheckboxTooltip = viewItem.CheckboxTooltip;
                    lvi.SetImageType(viewItem.ImageType);
                    lvi.ActionTooltip = viewItem.ActionTooltip;
                }

                if (defActionViewItem == null)
                {
                    if (defActionItem != null)
                    {
                        list.Items.Remove(defActionItem);
                    }
                }
                else
                {
                    if (defActionItem == null)
                    {
                        defActionItem = new FilterListViewItem(defActionViewItem);
                        list.Items.Insert(nextListViewItemIndex, defActionItem);
                        defActionItem.IsChecked = null;
                    }
                    ++nextListViewItemIndex;
                    defActionItem.Text = defActionViewItem.ToString();
                    defActionItem.SetImageType(defActionViewItem.ImageType);
                }
                while (list.Items.Count > nextListViewItemIndex)
                {
                    list.Items.RemoveAt(list.Items.Count - 1);
                }
            }
            finally
            {
                updateLock--;
                list.EndUpdate();
            }
        }

        private void list_Layout(object sender, LayoutEventArgs e)
        {
            itemColumnHeader.Width = list.ClientSize.Width - 15;
        }

        private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (updateLock > 0)
                return;
            viewModel.OnItemChecked(GetFilterListViewItem(e.Item)?.ViewItem, e.Item.Checked);
        }

        private void list_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) && (e.Clicks > 1))
            {
                ignoreNextCheck = true;
                viewModel.OnDoubleClicked();
            }
        }

        private void list_MouseMove(object sender, MouseEventArgs e)
        {
            var ht = list.HitTest(e.Location);
            var item = GetFilterListViewItem(ht?.Item);
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
            viewModel.OnContextMenuOpening(out enabledItems, out checkedItems);

            filterEnabledToolStripMenuItem.Enabled = (enabledItems & ContextMenuItem.FilterEnabled) != 0;
            propertiesToolStripMenuItem.Enabled = (enabledItems & ContextMenuItem.Properties) != 0;

            filterEnabledToolStripMenuItem.Checked = (checkedItems & ContextMenuItem.FilterEnabled) != 0;
        }

        private void filterEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewModel.OnFilterEnabledMenuItemClicked();
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewModel.OnPropertiesMenuItemClicked();
        }

        private void list_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (ignoreNextCheck)
            {
                e.NewValue = e.CurrentValue;
                ignoreNextCheck = false;
            }
            else if (GetFilterListViewItem(e.Index)?.IsCheckable != true)
            {
                e.NewValue = CheckState.Checked;
            }
        }

        private void list_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewModel.OnChangeSelection(list.SelectedItems.OfType<ListViewItem>().Select(
                i => GetFilterListViewItem(i)?.ViewItem).Where(i => i != null).ToArray());
        }

        private void list_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                viewModel.OnEnterPressed();
            else if (e.KeyCode == Keys.Delete)
                viewModel.OnDeletePressed();
        }

        class FilterListViewItem : ListViewItem
        {
            public bool IsCheckable = true;
            Color? color;

            public FilterListViewItem(IViewItem item)
            {
                this.ViewItem = item;
                this.Name = item.Key;
                if (item.IsDefaultActionItem)
                    this.ForeColor = System.Drawing.SystemColors.GrayText;
            }

            public IViewItem ViewItem { get; private set; }

            public void SetImageType(ViewItemImageType value)
            {
                if (value == ViewItemImageType.Exclude)
                    ImageIndex = 0;
                else if (value == ViewItemImageType.Include)
                    ImageIndex = 2;
                else
                    ImageIndex = 1;
            }
            public bool? IsChecked
            {
                get { return base.Checked; }
                set { base.Checked = value.GetValueOrDefault(true); IsCheckable = value != null; }
            }

            public Color? Color
            {
                get { return color; }
                set
                {
                    color = value;
                    BackColor = color != null ? color.Value.ToSystemDrawingObject() : System.Drawing.Color.Empty;
                }
            }

            public string CheckboxTooltip { get; set; }

            public string ActionTooltip { get; set; }
        };
    }
}
