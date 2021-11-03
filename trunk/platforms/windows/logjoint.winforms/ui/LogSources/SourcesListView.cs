using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesList;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl, IView
	{
		IViewModel viewModel;
		bool updateLocked;
		Windows.Reactive.ITreeViewController<IViewItem> treeViewController;

		public SourcesListView()
		{
			InitializeComponent();
		}

		public void Init(Windows.Reactive.IReactive reactive)
		{
			treeViewController = reactive.CreateTreeViewController<IViewItem>(treeView);

			treeViewController.OnExpand = item => viewModel.OnItemExpand(item);
			treeViewController.OnCollapse = item => viewModel.OnItemCollapse(item);
			treeViewController.OnSelect = items => viewModel.OnSelectionChange(items);
			treeViewController.OnUpdateNode = (treeNode, item, oldItem) =>
			{
				var vi = item;
				var oldvi = oldItem;
				updateLocked = true;
				if (vi.Checked != oldvi?.Checked)
					treeNode.Checked = vi.Checked == true;
				if (vi.ToString() != oldvi?.ToString())
					treeNode.Text = vi.ToString();
				if (vi.Color != oldvi?.Color)
					treeNode.BackColor = Drawing.PrimitivesExtensions.ToSystemDrawingObject(vi.Color.value);
				updateLocked = false;
			};
		}

		public void SetViewModel(IViewModel value)
		{
			this.viewModel = value;
			this.viewModel.SetView(this);

			var updateTree = Updaters.Create(
				() => value.RootItem,
				treeViewController.Update
			);

			var updateFocusedMessage = Updaters.Create(
				() => value.FocusedMessageItem,
				_ => treeView.Invalidate(new Rectangle(0, 0, (int)Math.Ceiling(UIUtils.FocusedItemMarkBounds.Width), treeView.Height))
			);

			value.ChangeNotification.CreateSubscription(() =>
			{
				updateTree();
				updateFocusedMessage();
			});
		}

		void IView.SetTopItem(IViewItem item)
		{
			var node = treeViewController.Map(item);
			if (node != null)
				treeView.TopNode = node;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			var (visibleItems, checkedItems) = viewModel.OnMenuItemOpening((ModifierKeys & Keys.Control) != 0);

			sourceVisisbleMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceVisible) != 0;
			saveLogAsToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SaveLogAs) != 0;
			sourceProprtiesMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceProperties) != 0;
			separatorToolStripMenuItem1.Visible = (visibleItems & Presenters.SourcesList.MenuItem.Separator1) != 0;
			openContainingFolderToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.OpenContainingFolder) != 0;
			saveMergedFilteredLogToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SaveMergedFilteredLog) != 0;
			showOnlyThisSourceMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.ShowOnlyThisLog) != 0;
			showAllSourcesMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.ShowAllLogs) != 0;
			copyErrorMessageMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.CopyErrorMessage) != 0;
			closeOthersMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.CloseOthers) != 0;

			sourceVisisbleMenuItem.Checked = (checkedItems & Presenters.SourcesList.MenuItem.SourceVisible) != 0;

			if (visibleItems == Presenters.SourcesList.MenuItem.None)
				e.Cancel = true;
		}

		private void sourceProprtiesMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnSourceProprtiesMenuItemClicked();
		}

		private void sourceVisisbleMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnSourceVisisbleMenuItemClicked(sourceVisisbleMenuItem.Checked);
		}

		private void showOnlyThisSourceMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnShowOnlyThisLogClicked();
		}

		private void showAllSourcesMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnShowAllLogsClicked();
		}

		private void closeOthersMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnCloseOthersClicked();
		}

		private void copyErrorMessageMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnCopyErrorMessageClicked();
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				viewModel.OnEnterKeyPressed();
			}
			else if (e.KeyCode == Keys.Delete)
			{
				viewModel.OnDeleteButtonPressed();
			}
			else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
			{
				viewModel.OnCopyShortcutPressed();
			}
			else if (e.KeyCode == Keys.Insert && e.Modifiers == Keys.Control)
			{
				viewModel.OnCopyShortcutPressed();
			}
			else if (e.KeyCode == Keys.A && e.Control)
			{
				viewModel.OnSelectAllShortcutPressed();
			}
		}

		private void saveLogAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnSaveLogAsMenuItemClicked();
		}

		private void saveMergedFilteredLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnSaveMergedFilteredLogMenuItemClicked();
		}

		private void openContainingFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			viewModel.OnOpenContainingFolderMenuItemClicked();
		}

		private void treeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			if (updateLocked)
				return;
			e.Cancel = true;
			viewModel.OnItemCheck(treeViewController.Map(e.Node), !e.Node.Checked);
		}

		private void treeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			var item = viewModel?.FocusedMessageItem;
			if (item != null)
			{
				if (treeViewController.Map(e.Node) == item)
				{
					var bounds = e.Bounds;
					UIUtils.DrawFocusedItemMark(e.Graphics, 1, (bounds.Top + bounds.Bottom) / 2);
				}
			}
		}
	}
	
}
