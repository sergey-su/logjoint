using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.ThreadsList;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class ThreadsListView : UserControl, System.Collections.IComparer, IView
	{
		Presenter presenter;
		readonly int scrollBarWidth = 30;

		public ThreadsListView()
		{
			InitializeComponent();

			firstMsgColumn.Width = lastMsgColumn.Width = UIUtils.Dpi.ScaleUp(175, 120);
			scrollBarWidth = UIUtils.Dpi.ScaleUp(scrollBarWidth, 120);
		}

		public void SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;
		}

		public void BeginBulkUpdate()
		{
			list.BeginUpdate();
		}

		public void EndBulkUpdate()
		{
			list.EndUpdate();
		}

		public IEnumerable<IViewItem> Items
		{
			get 
			{
				foreach (ListViewItem lvi in list.Items)
				{
					yield return Get(lvi);
				}
			}
		}

		public void RemoveItem(IViewItem item)
		{
			list.Items.Remove(Get(item));
		}

		public IViewItem Add(IThread thread)
		{
			return new ViewItem(list, thread, presenter.Theme);
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IViewItem TopItem 
		{
			get { return list.TopItem != null ? Get(list.TopItem) : null; }
			set { list.TopItem = value != null ? Get(value) : null; }
		}

		public void SortItems()
		{
			if (list.ListViewItemSorter == null)
				list.ListViewItemSorter = this;
			else
				list.Sort();
			list.Refresh();
		}

		public void UpdateFocusedThreadView()
		{
			list.Invalidate(new Rectangle(0, 0, 5, Height));
		}

		ViewItem Get(int idx)
		{
			if (idx >= list.Items.Count || idx < 0)
				return null;
			return Get(list.Items[idx]);
		}

		ViewItem Get(ListViewItem lvi)
		{
			return lvi.Tag as ViewItem;
		}

		ListViewItem Get(IViewItem vi)
		{
			return ((ViewItem)vi).ListViewItem;
		}

		ViewItem Get()
		{
			foreach (ListViewItem lvi in list.SelectedItems)
				return Get(lvi);
			return null;
		}

		public void SetThreadsDiscoveryState(bool inProgress) { }

		void list_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			if (e.ColumnIndex == 0)
			{
				if (presenter.IsThreadFocused(Get(e.Item).Thread))
				{
					UIUtils.DrawFocusedItemMark(e.Graphics, e.Bounds.X + 1, (e.Bounds.Top + e.Bounds.Bottom) / 2);
				}
				e.DrawDefault = true;
			}
			else
			{
				IThread thread = Get(e.ItemIndex).Thread;
				if (thread.IsDisposed)
					return;
				e.Graphics.FillRectangle(UIUtils.GetPaletteColorBrush(presenter.Theme.ThreadColors.GetByIndex(thread.ThreadColorIndex)), e.Bounds);
				e.DrawText(TextFormatFlags.Left);
			}
		}

		void list_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			var sortingInfo = presenter.GetSortingInfo();

			bool imageVisible = sortingInfo.HasValue && e.ColumnIndex == sortingInfo.Value.SortColumn;
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
				imageList1.Draw(e.Graphics, imagePos, sortingInfo.Value.Ascending ? 0 : 1);
			}
			
			TextRenderer.DrawText(e.Graphics, e.Header.Text,
				this.Font, textBounds, this.ForeColor, 
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
		}

		void list_Layout(object sender, LayoutEventArgs e)
		{
			idColumn.Width = ClientSize.Width - scrollBarWidth - 
				(firstMsgColumn.Width + lastMsgColumn.Width);
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

		void list_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;
			IBookmark bmk = FindSubItemWithBookmark(e.Location);
			if (bmk != null)
				presenter.OnBookmarkClicked(bmk);
		}

		void list_MouseMove(object sender, MouseEventArgs e)
		{
			if (FindSubItemWithBookmark(this.PointToClient(MousePosition)) != null)
				list.Cursor = Cursors.Hand;
			else
				list.Cursor = Cursors.Default;
		}

		void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			var item = Get();
			if (item == null || item.Thread.IsDisposed)
			{
				e.Cancel = true;
			}
		}

		void list_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			presenter.OnListColumnClicked(e.Column);
		}

		private void discoverThreadsMenuItem_Click(object sender, EventArgs e)
		{
			var item = Get();
			if (item != null)
				presenter.OnDiscoverThreadsMenuItemClicked(item);
		}


		#region IComparer Members

		public int Compare(object x, object y)
		{
			IThread t1 = Get((ListViewItem)x).Thread;
			IThread t2 = Get((ListViewItem)y).Thread;
			return presenter.CompareThreads(t1, t2);
		}

		#endregion

		void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ExecutePropsDialog();
		}

		void ExecutePropsDialog()
		{
			var item = Get();
			if (item != null)
				presenter.OnThreadPropertiesMenuItemClicked(item);
		}

		void list_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				ExecutePropsDialog();
			}
		}

		class ViewItem : IViewItem
		{
			public ViewItem(ListView lv, IThread thread, Presenters.IColorTheme theme)
			{
				var lvi = new ListViewItem();

				this.lvi = lvi;
				this.thread = thread;
				this.lvi.Tag = this;
				
				lvi.SubItems.Add("");
				lvi.SubItems.Add("");
				lvi.SubItems.Add("");

				lvi.BackColor = Drawing.PrimitivesExtensions.ToSystemDrawingObject(theme.ThreadColors.GetByIndex(thread.ThreadColorIndex));
				lvi.UseItemStyleForSubItems = true;

				lv.Items.Add(lvi);
			}

			public ListViewItem ListViewItem
			{
				get { return lvi; }
			}

			public IThread Thread
			{
				get { return thread; }
			}

			public string Text
			{
				get { return lvi.Text; }
				set
				{
					if (lvi.Text != value)
						lvi.Text = value;
				}
			}

			public void SetSubItemText(int subItemIdx, string value)
			{
				var subItem = lvi.SubItems[subItemIdx];
				if (subItem.Text != value)
					subItem.Text = value;
				subItem.Tag = null;
			}

			public void SetSubItemBookmark(int subItemIdx, IBookmark bmk)
			{
				var i = lvi.SubItems[subItemIdx];

				i.Tag = bmk;
				string newTxt;
				bool newIsLink;
				if (bmk != null)
				{
					newTxt = bmk.Time.ToUserFrendlyString(false);
					newIsLink = true;
				}
				else
				{
					newTxt = "-";
					newIsLink = false;
				}
				if (i.Text != newTxt)
					i.Text = newTxt;
				bool isLink = (i.Font.Style & FontStyle.Underline) != 0;
				if (isLink != newIsLink)
				{
					if (newIsLink)
					{
						i.Font = new Font(lvi.ListView.Font, FontStyle.Underline);
						i.ForeColor = Color.Blue;
					}
					else
					{
						i.Font = lvi.ListView.Font;
						i.ForeColor = lvi.ListView.ForeColor;
					}
				}
			}

			public bool Checked
			{
				get
				{
					return lvi.Checked;
				}
				set
				{
					if (lvi.Checked != value)
						lvi.Checked = value;
				}
			}

			public bool Selected 
			{
				get { return lvi.Selected; }
				set { if (value != lvi.Selected) lvi.Selected = value; }
			}

			readonly ListViewItem lvi;
			readonly IThread thread;
		};
	}
}
