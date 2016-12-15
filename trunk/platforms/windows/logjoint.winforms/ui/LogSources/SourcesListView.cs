using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesList;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl, IView
	{
		IViewEvents presenter;
		bool refreshColumnHeaderPosted;
		bool updateLocked;
		readonly int listNonClientWidth;

		public SourcesListView()
		{
			InitializeComponent();
			this.DoubleBuffered = true;
			this.listNonClientWidth = list.Width - list.ClientSize.Width;
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.BeginUpdate()
		{
			updateLocked = true;
			list.BeginUpdate();
		}

		void IView.EndUpdate()
		{
			list.EndUpdate();
			updateLocked = false;
		}

		IEnumerable<IViewItem> IView.Items
		{
			get { return list.Items.OfType<ViewItem>().SelectMany(i => i.GetSelfAndChildrenIfTopLevel()); }
		}

		IViewItem IView.AddItem(object datum, IViewItem parent)
		{
			var item = new ViewItem(datum, list, Cast(parent), treeControlsColumnHeader);
			return item;
		}

		void IView.Remove(IViewItem item)
		{
			var lvi = Cast(item);
			if (lvi == null)
				return;
			lvi.Cleanup();
		}

		void IView.SetTopItem(IViewItem item)
		{
			list.TopItem = Cast(item);
		}

		void IView.InvalidateFocusedMessageArea()
		{
			list.Invalidate(new Rectangle(0, 0, 5, Height));
		}

		string IView.ShowSaveLogDialog(string suggestedLogFileName)
		{
			var dlg = saveFileDialog1;
			//dlg.Filter = "*.log|*.log|*.*|*.*";
			dlg.FileName = suggestedLogFileName;
			if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return null;
			return dlg.FileName;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
				return cp;
			}
		}

		static class Native
		{
			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
			public const int WM_USER = 0x0400;
		}
		public const int WM_REFRESHCULUMNHEADER = Native.WM_USER + 502;

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			PostColumnHeaderUpdate();
		}

		private void list_MouseDown(object sender, MouseEventArgs e)
		{
			var item = Cast(list.GetItemAt(e.X, e.Y));
			if (item?.GetSubItemAt(e.X, e.Y)?.Tag == treeControlsColumnHeader && item.IsExpandable)
			{
				updateLocked = true;
				if (item.IsExpanded)
					item.Collapse();
				else
					item.Expand();
				updateLocked = false;
			}
		}

		private void PostColumnHeaderUpdate()
		{
			if (!refreshColumnHeaderPosted)
			{
				Native.PostMessage(this.Handle, WM_REFRESHCULUMNHEADER, IntPtr.Zero, IntPtr.Zero);
				refreshColumnHeaderPosted = true;
			}
		}

		void RefreshColumnHeader()
		{
			itemColumnHeader.Width = Math.Max(5, 
				list.Width 
				- treeControlsColumnHeader.Width - currentSourceMarkColumnHeader.Width 
				- SystemInformation.VerticalScrollBarWidth 
				- listNonClientWidth
				- 2
			);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_REFRESHCULUMNHEADER)
			{
				refreshColumnHeaderPosted = false;
				RefreshColumnHeader();
				return;
			}
			base.WndProc(ref m);
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (updateLocked)
				return;
			presenter.OnItemChecked(Cast(e.Item));
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			Presenters.SourcesList.MenuItem visibleItems, checkedItems;
			presenter.OnMenuItemOpening((ModifierKeys & Keys.Control) != 0, out visibleItems, out checkedItems);

			sourceVisisbleMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceVisible) != 0;
			saveLogAsToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SaveLogAs) != 0;
			sourceProprtiesMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceProprties) != 0;
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
			presenter.OnSourceProprtiesMenuItemClicked();
		}

		private void sourceVisisbleMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnSourceVisisbleMenuItemClicked(sourceVisisbleMenuItem.Checked);
		}

		private void showOnlyThisSourceMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnShowOnlyThisLogClicked();
		}

		private void showAllSourcesMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnShowAllLogsClicked();
		}

		private void closeOthersMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnCloseOthersClicked();
		}

		private void copyErrorMessageMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnCopyErrorMessageCliecked();
		}

		private void list_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			e.DrawDefault = true;

			if (e.ColumnIndex == 1)
			{
				e.DrawDefault = false;
				var item = Cast(e.Item);
				if (item == null || !item.IsExpandable)
					return;
				var bounds = e.SubItem.Bounds;
				float collapseExpandTriangleSize = bounds.Height / 2 - 1;
				var points = new PointF[]
				{
					new PointF(-collapseExpandTriangleSize/2, -collapseExpandTriangleSize/2),
					new PointF(collapseExpandTriangleSize/2, 0),
					new PointF(-collapseExpandTriangleSize/2, collapseExpandTriangleSize/2),
				};
				var state = e.Graphics.Save();
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				e.Graphics.TranslateTransform((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
				if (item.IsExpanded)
					e.Graphics.RotateTransform(90);
				e.Graphics.FillPolygon(Brushes.Black, points);
				e.Graphics.Restore(state);
				return;
			}

			if (e.ColumnIndex == 2)
			{
				e.DrawDefault = false;
				if (e.Item == Cast(presenter.OnFocusedMessageSourcePainting()))
				{
					var bounds = e.SubItem.Bounds;
					UIUtils.DrawFocusedItemMark(e.Graphics, bounds.X + 1, (bounds.Top + bounds.Bottom) / 2);
				}
				return;
			}

			if (e.ColumnIndex == 0)
			{
				// custom draw to ensure unfocused selected item has bright background instead of pale default one

				e.DrawDefault = false;

				var textRect = e.Item.Bounds;
				var offset = treeControlsColumnHeader.Width + currentSourceMarkColumnHeader.Width + 
					e.Item.IndentCount * dummyImageList.ImageSize.Width;
				textRect.X += offset;
				textRect.Width -= offset;

				using (var backColorBrush = new SolidBrush(e.Item.BackColor))
					e.Graphics.FillRectangle(e.Item.Selected ? SystemBrushes.Highlight : backColorBrush, textRect);

				var viewItem = e.Item as IViewItem;
				if (viewItem != null && viewItem.Checked.HasValue)
				{
					Rectangle cbRect = new Rectangle(textRect.Left, textRect.Top, textRect.Height, textRect.Height);
					cbRect.Inflate(-2, -2);
					ControlPaint.DrawCheckBox(e.Graphics, cbRect, ButtonState.Flat |
						(viewItem.Checked.GetValueOrDefault() ? ButtonState.Checked : ButtonState.Normal));
				}

				textRect.X += textRect.Height;
				textRect.Width -= textRect.Height;
				var textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
				TextRenderer.DrawText(e.Graphics, e.SubItem.Text,
					e.Item.Font, textRect, e.Item.Selected ? SystemColors.HighlightText : e.Item.ForeColor, textFlags);

				return;
			}
		}

		private void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				presenter.OnEnterKeyPressed();
			}
			else if (e.KeyCode == Keys.Delete)
			{
				presenter.OnDeleteButtonPressed();
			}
			else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
			{
				presenter.OnCopyShortcutPressed();
			}
			else if (e.KeyCode == Keys.Insert && e.Modifiers == Keys.Control)
			{
				presenter.OnCopyShortcutPressed();
			}
			else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left)
			{
				bool expand = e.KeyCode == Keys.Right;
				updateLocked = true;
				var selection = list.SelectedItems.OfType<ViewItem>().ToArray();
				foreach (var i in selection)
					if (expand)
						i.Expand();
					else
						i.Collapse();
				if (selection.Length == 1 && !expand && !selection[0].IsTopLevel && selection[0].Parent.IsExpanded)
				{
					list.SelectedIndices.Clear();
					var newSelection = selection[0].Parent;
					list.SelectedIndices.Add(newSelection.Index);
					list.FocusedItem = newSelection;
					list.TopItem = newSelection;
				}
				updateLocked = false;
			}
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			// preprocessings shound not have checkboxes at all, but it is impossible to hide the checkboxes.
			// let's make them not uncheckable.
			if ((Cast(list.Items[e.Index]) as IViewItem)?.Checked == null)
				e.NewValue = CheckState.Unchecked;
		}

		private void saveLogAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnSaveLogAsMenuItemClicked();
		}

		private void saveMergedFilteredLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnSaveMergedFilteredLogMenuItemClicked();
		}

		private void openContainingFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnOpenContainingFolderMenuItemClicked();
		}

		private async void list_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			await Task.Yield();
			// repost the notification to message queue to let ListViewItem's properties be updated.
			// reading ListViewItem.Selected from here (or down the stack) gives wrong values.
			presenter.OnSelectionChanged();
		}

		static ViewItem Cast(IViewItem item)
		{
			return (ViewItem)item;
		}

		static ViewItem Cast(ListViewItem item)
		{
			return (ViewItem)item;
		}

		class ViewItem : ListViewItem, IViewItem
		{
			object datum;
			bool isCheckedValid;
			ListView list;
			ViewItem parent;
			bool expanded;
			List<ViewItem> childen = new List<ViewItem>();

			public ViewItem(object datum, ListView list, ViewItem parent, object treeControlCellTag)
			{
				this.datum = datum;
				this.parent = parent;
				this.list = list;
				this.SubItems.Add(new ListViewSubItem() { Tag = treeControlCellTag });
				this.SubItems.Add(new ListViewSubItem());
				this.expanded = false;
				if (parent != null)
				{
					parent.childen.Add(this);
					this.IndentCount = 12;
				}
				else
				{
					list.Items.Add(this);
				}
			}

			public bool IsTopLevel
			{
				get { return parent == null; }
			}

			public ViewItem Parent
			{
				get { return parent; }
			}

			public bool IsExpandable
			{
				get { return IsTopLevel && childen.Count > 0; }
			}

			public bool IsExpanded
			{
				get { return expanded; }
			}

			public IEnumerable<ViewItem> GetSelfAndChildrenIfTopLevel()
			{
				if (IsTopLevel)
				{
					yield return this;
					foreach (var c in childen)
						yield return c;
				}
			}

			public void Cleanup()
			{
				list.Items.Remove(this);
				childen.ForEach(c => list.Items.Remove(c));
				childen.Clear();
			}

			public bool Expand()
			{
				if (!IsExpandable || expanded)
					return false;
				expanded = true;
				list.BeginUpdate();
				int idx = 1;
				foreach (var c in childen)
				{
					list.Items.Insert(this.Index + idx, c);
					++idx;
				}
				list.EndUpdate();
				return true;
			}

			public bool Collapse()
			{
				if (!IsExpandable || !expanded)
					return false;
				expanded = false;
				list.BeginUpdate();
				foreach (var c in childen)
					list.Items.Remove(c);
				list.EndUpdate();
				return true;
			}

			object IViewItem.Datum
			{
				get { return datum; }
			}

			bool? IViewItem.Checked
			{
				get { return isCheckedValid ? base.Checked : new bool?(); }
				set { isCheckedValid = value != null; base.Checked = value.GetValueOrDefault(); }
			}

			void IViewItem.SetText(string value)
			{
				base.Text = value;
			}

			void IViewItem.SetBackColor(ModelColor color, bool isErrorColor)
			{
				base.BackColor = color.ToColor();
			}

			bool IViewItem.Selected
			{
				get { return base.Selected; }
				set { base.Selected = value; }
			}
		};
	}
	
}
