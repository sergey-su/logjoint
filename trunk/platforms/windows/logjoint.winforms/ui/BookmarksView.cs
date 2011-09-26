using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class BookmarksView : UserControl
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

		public void SetHost(IBookmarksViewHost host)
		{
			this.host = host;
		}

		public void UpdateView()
		{
			listBox.Items.Clear();
			foreach (IBookmark bmk in host.Bookmarks.Items)
				listBox.Items.Add(bmk);
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
				ClickSelectedLink();
		}

		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.White, e.Bounds);
			if (e.Index < 0)
				return; // DrawItem sometimes called even when no item in the list :(
			var imgSize = imageList1.ImageSize;
			imageList1.Draw(e.Graphics, 
				e.Bounds.X + (iconAreaWidth - imgSize.Width) / 2,
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
		}

		private void listBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				ClickSelectedLink();
		}

		private void listBox1_MouseDown(object sender, MouseEventArgs e)
		{
			int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false);
			if (linkUnderMouse == null)
				return;
			if (e.Button == MouseButtons.Left)
			{
				host.NavigateTo(Get(linkUnderMouse.Value));
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

		IBookmark Get()
		{
			return Get(listBox.SelectedIndex);
		}

		void ClickSelectedLink()
		{
			var bmk = Get();
			if (bmk != null)
				host.NavigateTo(bmk);
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var bmk = Get();
			if (bmk != null)
			{
				host.Bookmarks.ToggleBookmark(bmk);
				UpdateView();
			}
		}

		private void contextMenu_Opening(object sender, CancelEventArgs e)
		{
			if (Get() == null)
				e.Cancel = true;
		}

		const int iconAreaWidth = 16;
		private IBookmarksViewHost host;
		private Font displayFont;
		private StringFormat displayStringFormat;
	}

}
