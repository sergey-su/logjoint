using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesList;
using ILogSourcePreprocessing = LogJoint.Preprocessing.ILogSourcePreprocessing;

namespace LogJoint.UI
{
	public partial class SourcesListView : UserControl, IView
	{
		IPresenterEvents presenter;
		bool refreshColumnHeaderPosted;

		public SourcesListView()
		{
			InitializeComponent();
			this.DoubleBuffered = true;
		}

		void IView.SetPresenter(IPresenterEvents presenter)
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
			dlg.FileName = suggestedLogFileName;
			if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return null;
			return dlg.FileName;
		}

		void IView.ShowSaveLogError(string msg)
		{
			MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
				Native.PostMessage(this.Handle, WM_REFRESHCULUMNHEADER, IntPtr.Zero, IntPtr.Zero);
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

		private void list_SelectedIndexChanged(object sender, EventArgs e)
		{
			presenter.OnSelectionChanged();
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			presenter.OnItemChecked(Cast(e.Item));
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			Presenters.SourcesList.MenuItem visibleItems, checkedItems;
			presenter.OnMenuItemOpening(out visibleItems, out checkedItems);

			sourceVisisbleMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceVisisble) != 0;
			saveLogAsToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SaveLogAs) != 0;
			sourceProprtiesMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.SourceProprties) != 0;
			separatorToolStripMenuItem1.Visible = (visibleItems & Presenters.SourcesList.MenuItem.Separator1) != 0;
			openContainingFolderToolStripMenuItem.Visible = (visibleItems & Presenters.SourcesList.MenuItem.OpenContainingFolder) != 0;
			saveMergedFilteredLogToolStripMenuItem.Enabled = (visibleItems & Presenters.SourcesList.MenuItem.SaveMergedFilteredLog) != 0;

			sourceVisisbleMenuItem.Checked = (checkedItems & Presenters.SourcesList.MenuItem.SourceVisisble) != 0; ;
		}

		private void sourceProprtiesMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnSourceProprtiesMenuItemClicked();
		}

		private void sourceVisisbleMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnSourceVisisbleMenuItemClicked(sourceVisisbleMenuItem.Checked);
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
			e.DrawDefault = true;
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

			bool IViewItem.Checked
			{
				get { return base.Checked; }
				set { base.Checked = value; }
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
