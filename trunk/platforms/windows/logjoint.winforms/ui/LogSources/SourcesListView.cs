using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesList;
using ILogSourcePreprocessing = LogJoint.Preprocessing.ILogSourcePreprocessing;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl, IView
	{
		IViewEvents presenter;
		bool refreshColumnHeaderPosted;

		public SourcesListView()
		{
			InitializeComponent();
			this.DoubleBuffered = true;
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		void IView.BeginUpdate()
		{
			list.BeginUpdate();
		}

		void IView.EndUpdate()
		{
			list.EndUpdate();
		}

		IViewItem IView.CreateItem(string key, ILogSource logSource, ILogSourcePreprocessing logSourcePreprocessing)
		{
			return new ViewItem(key, logSource, logSourcePreprocessing);
		}

		int IView.ItemsCount
		{
			get { return list.Items.Count; }
		}

		IViewItem IView.GetItem(int idx)
		{
			return Cast(list.Items[idx]);
		}

		void IView.RemoveAt(int idx)
		{
			list.Items.RemoveAt(idx);
		}

		int IView.IndexOfKey(string key)
		{
			return list.Items.IndexOfKey(key);
		}

		void IView.Add(IViewItem item)
		{
			list.Items.Add(Cast(item));
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

		void IView.ShowSaveLogError(string msg)
		{
			MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
			if (!refreshColumnHeaderPosted)
			{
				//Native.PostMessage(this.Handle, WM_REFRESHCULUMNHEADER, IntPtr.Zero, IntPtr.Zero);
				refreshColumnHeaderPosted = true;
			}
		}

		void RefreshColumnHeader()
		{
			itemColumnHeader.Width = list.ClientSize.Width - 10;
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

		private void list_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			ILogSource sourceToPaintAsFocused;
			presenter.OnFocusedMessageSourcePainting(out sourceToPaintAsFocused);
			var ls = Cast(e.Item).LogSource;
			if (ls != null && ls == sourceToPaintAsFocused)
			{
				UIUtils.DrawFocusedItemMark(e.Graphics, e.Bounds.X + 1, (e.Bounds.Top + e.Bounds.Bottom) / 2);
			}
		}

		private void list_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			Rectangle textRect = e.Bounds;
			textRect.Offset(5, 0);

			using (var backColorBrush = new SolidBrush(e.Item.BackColor))
			{
				if (e.Item.Selected)
				{
					e.Graphics.FillRectangle(backColorBrush, new Rectangle(e.Bounds.Location, new Size(textRect.Width, e.Bounds.Height)));
					e.Graphics.FillRectangle(SystemBrushes.Highlight, textRect);
				}
				else
				{
					e.Graphics.FillRectangle(backColorBrush, e.Bounds);
				}
			}
			var viewItem = e.Item as IViewItem;
			if (viewItem != null && viewItem.Checked.HasValue)
			{
				Rectangle cbRect = new Rectangle(textRect.Left, textRect.Top, textRect.Height, textRect.Height);
				cbRect.Inflate(-2, -2);
				ControlPaint.DrawCheckBox(e.Graphics, cbRect,
					viewItem.Checked.GetValueOrDefault() ? ButtonState.Checked : ButtonState.Normal);
			}
			textRect.X += textRect.Height;
			textRect.Width -= textRect.Height;
			var textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
			if (e.Item.Selected)
			{
				TextRenderer.DrawText(e.Graphics, e.SubItem.Text,
					e.Item.Font, textRect, SystemColors.HighlightText, textFlags);
			}
			else
			{
				TextRenderer.DrawText(e.Graphics, e.SubItem.Text,
					e.Item.Font, textRect, e.Item.ForeColor, textFlags);
			}

			ILogSource sourceToPaintAsFocused;
			presenter.OnFocusedMessageSourcePainting(out sourceToPaintAsFocused);
			var ls = Cast(e.Item).LogSource;
			if (ls != null && ls == sourceToPaintAsFocused)
			{
				UIUtils.DrawFocusedItemMark(e.Graphics, e.Bounds.X + 1, (e.Bounds.Top + e.Bounds.Bottom) / 2);
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
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			// prepeocessings shound not have checkboxes at all, but it is impossible to hide the checkboxes.
			// let's make them not uncheckable.
			if (Cast(list.Items[e.Index]).LogSourcePreprocessing != null)
				e.NewValue = CheckState.Checked;
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
			ILogSource logSource;
			ILogSourcePreprocessing logSourcePreprocessing;
			bool isCheckedValid;

			public ViewItem(string key, ILogSource logSource, ILogSourcePreprocessing logSourcePreprocessing)
			{
				this.Name = key;
				this.logSource = logSource;
				this.logSourcePreprocessing = logSourcePreprocessing;
			}

			public ILogSource LogSource
			{
				get { return logSource; }
			}

			public ILogSourcePreprocessing LogSourcePreprocessing
			{
				get { return logSourcePreprocessing; }
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

			void IViewItem.SetBackColor(ModelColor color)
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
