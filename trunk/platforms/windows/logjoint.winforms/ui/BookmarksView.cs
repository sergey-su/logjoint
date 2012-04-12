using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.BookmarksList;

namespace LogJoint.UI
{
	public partial class BookmarksView : UserControl, IView
	{
		public BookmarksView()
		{
			InitializeComponent();
			
			displayFont = new Font(listBox.Font, FontStyle.Underline);
			
			displayStringFormat = new StringFormat();
			displayStringFormat.Alignment = StringAlignment.Near;
			displayStringFormat.LineAlignment = StringAlignment.Near;
			displayStringFormat.Trimming = StringTrimming.EllipsisCharacter;
			displayStringFormat.FormatFlags = StringFormatFlags.NoWrap;
		}

		public void SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;
		}

		public void RemoveAllItems()
		{
			listBox.Items.Clear();
		}

		public int AddItem(IBookmark bmk)
		{
			return listBox.Items.Add(bmk);
		}

		public IBookmark SelectedBookmark { get { return Get(listBox.SelectedIndex); } }

		public void RefreshFocusedMessageMark()
		{
			var focusedItemMarkBounds = UIUtils.FocusedItemMarkBounds;
			listBox.Invalidate(new Rectangle(
				focusedMessageMarkX + focusedItemMarkBounds.Left,
				0,
				focusedItemMarkBounds.Width,
				ClientSize.Height));
		}

		int? GetLinkFromPoint(int x, int y, bool fullRowMode)
		{
			if (listBox.Items.Count == 0)
				return null;
			y -= listBox.GetItemRectangle(0).Top;
			int idx = y / listBox.ItemHeight;
			if (idx < 0 || idx >= listBox.Items.Count)
				return null;
			if (!fullRowMode)
			{
				var txt = listBox.Items[idx].ToString();
				using (var g = listBox.CreateGraphics())
				{
					if (x < iconAreaWidth || x > iconAreaWidth + g.MeasureString(txt, displayFont, listBox.ClientSize.Width - iconAreaWidth, displayStringFormat).Width)
						return null;
				}
			}
			return idx;
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			var pt = listBox.PointToClient(Control.MousePosition);
			if (GetLinkFromPoint(pt.X, pt.Y, true) != null)
				presenter.ViewDoubleClicked();
		}

		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.White, e.Bounds);
			if (e.Index < 0)
				return; // DrawItem sometimes called even when no item in the list :(
			var imgSize = imageList1.ImageSize;
			imageList1.Draw(e.Graphics,
				e.Bounds.X + iconPositionX,
				e.Bounds.Y + (e.Bounds.Height - imgSize.Height) / 2, 
				0);
			Rectangle textArea = e.Bounds;
			textArea.X += iconAreaWidth;
			textArea.Width -= iconAreaWidth;
			e.Graphics.DrawString(listBox.Items[e.Index].ToString(), displayFont, Brushes.Blue, textArea, displayStringFormat);
			if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.Focus) != 0)
			{
				ControlPaint.DrawFocusRectangle(e.Graphics, textArea, Color.Black, Color.White);
			}

			var focused = presenter.FocusedMessagePosition;
			if (focused != null)
			{
				int y;
				if (focused.Item1 != focused.Item2)
					y = listBox.ItemHeight * focused.Item1 + listBox.ItemHeight / 2;
				else
					y = listBox.ItemHeight * focused.Item1;
				if (y == 0)
					y = UIUtils.FocusedItemMarkBounds.Height / 2;
				UIUtils.DrawFocusedItemMark(e.Graphics, focusedMessageMarkX, y);
			}
		}

		private void listBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				presenter.HandleEnterKey();
		}

		private void listBox1_MouseDown(object sender, MouseEventArgs e)
		{
			int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false);
			if (linkUnderMouse == null)
				return;
			if (e.Button == MouseButtons.Left)
			{
				presenter.BookmarkLeftClicked(Get(linkUnderMouse.Value));
			}
			else if (e.Button == MouseButtons.Right)
			{
				listBox.SelectedIndex = linkUnderMouse.Value;
			}
		}

		private void listBox1_MouseMove(object sender, MouseEventArgs e)
		{
			int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false);
			listBox.Cursor = linkUnderMouse.HasValue ? Cursors.Hand : Cursors.Default;
		}

		IBookmark Get(int index)
		{
			if (index >= 0 && index < listBox.Items.Count)
				return listBox.Items[index] as IBookmark;
			return null;
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.DeleteMenuItemClicked();
		}

		private void contextMenu_Opening(object sender, CancelEventArgs e)
		{
			if (!presenter.ContextMenuMenuCanBeShown)
				e.Cancel = true;
		}

		const int iconAreaWidth = 20;
		const int iconPositionX = 2;
		const int focusedMessageMarkX = 17;
		private Presenter presenter;
		private Font displayFont;
		private StringFormat displayStringFormat;
	}

}
