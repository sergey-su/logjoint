using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ThreadsListView : UserControl, System.Collections.IComparer
	{
		int updateLock;
		IThreadsListViewHost host;
		int sortColumn = -1;
		bool ascending = false;

		public ThreadsListView()
		{
			InitializeComponent();
		}

		public event EventHandler ThreadChecked;

		public void SetHost(IThreadsListViewHost host)
		{
			this.host = host;
		}

		void SetBookmark(ListViewItem.ListViewSubItem i, IBookmark bmk)
		{
			i.Tag = bmk;
			string newTxt;
			bool newIsLink;
			if (bmk != null)
			{
				newTxt = bmk.Time.ToString();
				newIsLink = true;
			}
			else
			{
				newTxt = "-";
				newIsLink = false;
			}
			SetSubItemText(i, newTxt);
			bool isLink = (i.Font.Style & FontStyle.Underline) != 0;
			if (isLink != newIsLink)
			{
				if (newIsLink)
				{
					i.Font = new Font(this.Font, FontStyle.Underline);
					i.ForeColor = Color.Blue;
				}
				else
				{
					i.Font = this.Font;
					i.ForeColor = this.ForeColor;
				}
			}
		}

		void SetSubItemText(ListViewItem.ListViewSubItem item, string value)
		{
			if (item.Text != value)
				item.Text = value;
		}

		void SetChecked(ListViewItem lvi, bool checkedVal)
		{
			if (lvi.Checked != checkedVal)
				lvi.Checked = checkedVal;
		}

		public void UpdateView()
		{
			Dictionary<int, ListViewItem> existingThread = new Dictionary<int, ListViewItem>();
			foreach (ListViewItem lvi in list.Items)
			{
				existingThread.Add(Get(lvi).GetHashCode(), lvi);
			}
			++updateLock;
			try
			{
				foreach (ListViewItem lvi in existingThread.Values)
					if (Get(lvi).IsDisposed)
						list.Items.Remove(lvi);

				foreach (IThread t in host.Threads)
				{
					if (t.IsDisposed)
						continue;

					int hash = t.GetHashCode();
					ListViewItem lvi;
					if (!existingThread.TryGetValue(hash, out lvi))
					{
						lvi = new ListViewItem();
						lvi.Tag = t;
						lvi.SubItems.Add("-");
						lvi.SubItems.Add("-");
						lvi.SubItems.Add("-");
						lvi.BackColor = t.ThreadColor;
						lvi.UseItemStyleForSubItems = true;
						existingThread.Add(hash, lvi);
						list.Items.Add(lvi);
					}

					if (t.DisplayName != lvi.Text)
						lvi.Text = t.DisplayName;

					SetSubItemText(lvi.SubItems[3], t.MessagesCount.ToString());
					SetBookmark(lvi.SubItems[1], t.FirstKnownMessage);
					SetBookmark(lvi.SubItems[2], t.LastKnownMessage);
					SetChecked(lvi, t.ThreadMessagesAreVisible);
				}
			}
			finally
			{
				--updateLock;
			}
		}

		public void Select(IThread thread)
		{
			list.BeginUpdate();
			try
			{
				foreach (ListViewItem lvi in list.Items)
				{
					lvi.Selected = Get(lvi) == thread;
					if (lvi.Selected)
						list.TopItem = lvi;
				}
			}
			finally
			{
				list.EndUpdate();
			}
			
		}

		IThread Get(int idx)
		{
			if (idx >= list.Items.Count || idx < 0)
				return null;
			return Get(list.Items[idx]);
		}

		IThread Get(ListViewItem lvi)
		{
			return lvi.Tag as IThread;
		}

		IThread Get()
		{
			foreach (ListViewItem lvi in list.SelectedItems)
				return Get(lvi);
			return null;
		}

		protected virtual void OnNavigate(IBookmark bmk)
		{
			host.UINavigationHandler.ShowLine(bmk);
		}

		private void list_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			if (e.ColumnIndex == 0)
			{
				if (Get(e.Item) == host.FocusedMessageThread)
				{
					UIUtils.DrawFocusedItemMark(e.Graphics, e.Bounds.X + 1, (e.Bounds.Top + e.Bounds.Bottom) / 2);
				}
				e.DrawDefault = true;
			}
			else
			{
				IThread thread = Get(e.ItemIndex);
				if (thread.IsDisposed)
					return;
				e.Graphics.FillRectangle(thread.ThreadBrush, e.Bounds);
				e.DrawText(TextFormatFlags.Left);
			}
		}

		private void list_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			bool imageVisible = e.ColumnIndex == sortColumn;
			Rectangle textBounds = e.Bounds;
			if (imageVisible)
			{
				textBounds.X += imageList1.ImageSize.Width + 5;
			}


			ButtonState state = ButtonState.Normal;
			if (!this.Enabled)
			{
				state = ButtonState.Inactive;
			}
			else if ((e.State & ListViewItemStates.Selected) != 0)
			{
				state = ButtonState.Pushed;
				textBounds.X += 1;
				textBounds.Y += 1;
			}
			if (e.Bounds.Width > 0)
			{
				ControlPaint.DrawButton(e.Graphics, e.Bounds, state);
			}

			if (imageVisible)
			{
				Point imagePos = new Point(
					e.Bounds.X + 4, 
					(e.Bounds.Top + e.Bounds.Bottom - imageList1.ImageSize.Height) / 2
				);
				if ((e.State & ListViewItemStates.Selected) != 0)
					imagePos.X += 1;
				imageList1.Draw(e.Graphics, imagePos, ascending ? 0 : 1);
			}
			
			TextRenderer.DrawText(e.Graphics, e.Header.Text,
				this.Font, textBounds, this.ForeColor, 
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
		}

		private void list_Layout(object sender, LayoutEventArgs e)
		{
			idColumn.Width = ClientSize.Width - 30 - 
				(firstMsgColumn.Width + lastMsgColumn.Width + totalsColumn.Width);
		}

		private void list_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (updateLock != 0)
				return;
			IThread t = Get(e.Item);
			if (t != null && t.IsDisposed)
				return;
			if (t.LogSource != null && !t.LogSource.Visible)
				return;
			if (t.Visible == e.Item.Checked)
				return;
			t.Visible = e.Item.Checked;
			OnThreadChecked();
		}

		IBookmark FindSubItemWithBookmark(Point pt)
		{
			ListViewItem lvi = list.GetItemAt(pt.X, pt.Y);
			if (lvi == null)
				return null;
			ListViewItem.ListViewSubItem i = null;
			if (lvi.SubItems[1].Bounds.Contains(pt))
				i = lvi.SubItems[1];
			else
			if (lvi.SubItems[2].Bounds.Contains(pt))
				i = lvi.SubItems[2];
			if (i != null)
				if (i.Tag != null)
					return i.Tag as IBookmark;
			return null;
		}

		private void list_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;
			if (host == null)
				return;
			IBookmark bmk = FindSubItemWithBookmark(e.Location);
			if (bmk != null)
			{
				OnNavigate(bmk);
			}
		}

		private void list_MouseMove(object sender, MouseEventArgs e)
		{
			if (FindSubItemWithBookmark(this.PointToClient(MousePosition)) != null)
				list.Cursor = Cursors.Hand;
			else
				list.Cursor = Cursors.Default;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			IThread t = Get();
			if (t == null || t.IsDisposed)
			{
				e.Cancel = true;
			}
			else
			{
				visibleToolStripMenuItem.Checked = t.ThreadMessagesAreVisible;
				visibleToolStripMenuItem.Enabled = t.LogSource == null || t.LogSource.Visible;
			}
		}

		private void visibleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (updateLock != 0)
				return;
			IThread t = Get();
			if (t == null || t.IsDisposed)
				return;
			if (t.LogSource != null && !t.LogSource.Visible)
				return;
			visibleToolStripMenuItem.Checked = !visibleToolStripMenuItem.Checked;
			t.Visible = visibleToolStripMenuItem.Checked;
			OnThreadChecked();
		}

		private void list_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (e.Column == sortColumn)
			{
				ascending = !ascending;
			}
			else
			{
				sortColumn = e.Column;
			}
			if (list.ListViewItemSorter == null)
				list.ListViewItemSorter = this;
			else
				list.Sort();
			list.Refresh();
		}

		protected virtual void OnThreadChecked()
		{
			if (ThreadChecked != null)
				this.BeginInvoke(ThreadChecked, this, EventArgs.Empty);
		}

		#region IComparer Members

		static DateTime GetBookmarkDate(IBookmark bmk)
		{
			return bmk != null ? bmk.Time : DateTime.MinValue;
		}

		public int Compare(object x, object y)
		{
			IThread t1 = Get((ListViewItem)x);
			IThread t2 = Get((ListViewItem)y);
			if (t1.IsDisposed || t2.IsDisposed)
				return 0;
			int ret = 0;
			switch (sortColumn)
			{
				case 0:
					ret = string.Compare(t2.ID, t1.ID);
					break;
				case 1:
					ret = Math.Sign((GetBookmarkDate(t2.FirstKnownMessage) - GetBookmarkDate(t1.FirstKnownMessage)).Ticks);
					break;
				case 2:
					ret = Math.Sign((GetBookmarkDate(t2.LastKnownMessage) - GetBookmarkDate(t1.LastKnownMessage)).Ticks);
					break;
				case 3:
					ret = t2.MessagesCount - t1.MessagesCount;
					break;
			}
			return ascending ? ret : -ret;
		}

		#endregion

		private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IThread t = Get();
			if (t == null || t.IsDisposed)
				return;
			using (UI.ThreadPropertiesForm f = new UI.ThreadPropertiesForm(t, host.UINavigationHandler))
			{
				f.ShowDialog();
			}
		}

		private void list_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (updateLock != 0)
				return;
			IThread t = Get(e.Index);
			if (t == null || t.IsDisposed || (t.LogSource != null && !t.LogSource.Visible))
			{
				e.NewValue = e.CurrentValue;
			}
		}

		public void InvalidateFocusedMessageArea()
		{
			list.Invalidate(new Rectangle(0, 0, 5, Height));
		}
	}

	public interface IThreadsListViewHost
	{
		IEnumerable<IThread> Threads { get; }
		IUINavigationHandler UINavigationHandler { get; }
		IThread FocusedMessageThread { get; }
	};

	public class ThreadPropertiesEventArgs : EventArgs
	{
		public ThreadPropertiesEventArgs(IThread thread)
		{
			this.thread = thread;
		}

		public IThread Thread { get { return thread; } }

		IThread thread;
	};
}
