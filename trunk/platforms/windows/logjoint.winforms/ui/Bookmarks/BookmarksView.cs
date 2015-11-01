using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LogJoint.UI.Presenters.BookmarksList;

namespace LogJoint.UI
{
	public partial class BookmarksView : UserControl, IView
	{
		public BookmarksView()
		{
			InitializeComponent();

			linkDisplayFont = new Font(listBox.Font, FontStyle.Underline);
			timeDeltaDisplayFont = listBox.Font;
			
			displayStringFormat = new StringFormat();
			displayStringFormat.Alignment = StringAlignment.Near;
			displayStringFormat.LineAlignment = StringAlignment.Near;
			displayStringFormat.Trimming = StringTrimming.EllipsisCharacter;
			displayStringFormat.FormatFlags = StringFormatFlags.NoWrap;

			listBox.ItemHeight = UIUtils.Dpi.Scale(15);
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
			this.presentationDataAccess = presenter as IPresentationDataAccess;
		}

		void IView.UpdateItems(IEnumerable<ViewItem> items)
		{
			metrics = null;
			isUpdating = true;
			listBox.BeginUpdate();
			listBox.Items.Clear();
			foreach (var i in items)
			{
				var itemIdx = listBox.Items.Add(new BookmarkItem(i.Bookmark, i.Delta, i.AltDelta, i.IsEnabled));
				if (i.IsSelected)
					listBox.SelectedIndices.Add(itemIdx);
			}
			listBox.EndUpdate();
			isUpdating = false;
		}

		IBookmark IView.SelectedBookmark { get { return Get(listBox.SelectedIndex); } }

		IEnumerable<IBookmark> IView.SelectedBookmarks
		{
			get
			{
				foreach (int i in listBox.SelectedIndices)
					yield return Get(i);
			}
		}

		void IView.RefreshFocusedMessageMark()
		{
			var focusedItemMarkBounds = UIUtils.FocusedItemMarkBounds;
			listBox.Invalidate(new Rectangle(
				GetMetrics().FocusedMessageMarkX + focusedItemMarkBounds.Left,
				0,
				focusedItemMarkBounds.Width,
				ClientSize.Height));
		}

		void IView.Invalidate()
		{
			this.listBox.Invalidate();
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

		int? GetLinkFromPoint(int x, int y, bool fullRowMode, bool enabledOnly)
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
					var m = GetMetrics();
					if (x < m.TextX || x > m.TextX + g.MeasureString(txt, linkDisplayFont, listBox.ClientSize.Width - m.TextX, displayStringFormat).Width)
						return null;
				}
			}
			var item = GetItem(idx);
			if (item == null)
				return null;
			if (enabledOnly && !item.IsEnabled)
				return null;
			return idx;
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			var pt = listBox.PointToClient(Control.MousePosition);
			if (GetLinkFromPoint(pt.X, pt.Y, true, true) != null)
				presenter.OnViewDoubleClicked();
		}

		class Metrics
		{
			public int DeltaStringX;
			public int DeltaStringWidth;
			public int IconX;
			public int FocusedMessageMarkX;
			public int TextX;
		};

		Metrics CreateMetrics()
		{
			using (var g = this.CreateGraphics())
			{
				var m = new Metrics();
				m.DeltaStringX = 1;

				m.DeltaStringWidth = (int)
					 EnumItems()
					.Select(i => Math.Max(
						g.MeasureString(i.Delta ?? "", timeDeltaDisplayFont, new PointF(), displayStringFormat).Width,
						g.MeasureString(i.AltDelta ?? "", timeDeltaDisplayFont, new PointF(), displayStringFormat).Width
					))
					.Union(Enumerable.Repeat(0f, 1))
					.Max() + 2;

				m.IconX = m.DeltaStringX + m.DeltaStringWidth;
				m.FocusedMessageMarkX = m.IconX + imageList1.ImageSize.Width + 1;
				m.TextX = m.FocusedMessageMarkX + 4;
				
				return m;
			}
		}

		private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
		{
			var item = GetItem(e.Index);
			if (item == null)
			{
				e.Graphics.FillRectangle(Brushes.White, e.Bounds);
				return; // DrawItem sometimes called even when no item in the list :(
			}

			Brush bkBrush = Brushes.White;

			if ((e.State & DrawItemState.Selected) != 0)
			{
				bkBrush = selectedBkBrush;
			}
			else
			{
				var coloring = presentationDataAccess.Coloring;
				var thread = item.Bookmark.Thread;
				if (coloring == Settings.Appearance.ColoringMode.Threads)
					if (!thread.IsDisposed)
						bkBrush = thread.ThreadBrush;
				if (coloring == Settings.Appearance.ColoringMode.Sources)
					if (!thread.IsDisposed && !thread.LogSource.IsDisposed)
						bkBrush = thread.LogSource.SourceBrush;
			}

			e.Graphics.FillRectangle(bkBrush, e.Bounds);

			var m = GetMetrics();

			Rectangle r = e.Bounds;
			r.X = m.DeltaStringX;
			r.Width = m.DeltaStringWidth;

			var deltaStr = item.Delta;
			if (deltaStr != null)
			{
				e.Graphics.DrawString(
					deltaStr,
					timeDeltaDisplayFont,
					Brushes.Black,
					r,
					displayStringFormat);
			}

			var imgSize = imageList1.ImageSize;
			imageList1.Draw(e.Graphics,
				e.Bounds.X + m.IconX,
				e.Bounds.Y + (e.Bounds.Height - imgSize.Height) / 2, 
				0);

			r.X = m.TextX;
			r.Width = ClientSize.Width - m.TextX;
			e.Graphics.DrawString(item.Bookmark.ToString(), linkDisplayFont, 
				item.IsEnabled ? Brushes.Blue : Brushes.Gray, r, displayStringFormat);
			if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.Focus) != 0)
			{
				ControlPaint.DrawFocusRectangle(e.Graphics, r, Color.Black, Color.White);
			}
			Tuple<int, int> focused;
			presenter.OnFocusedMessagePositionRequired(out focused);
			if (focused != null)
			{
				int y;
				if (focused.Item1 != focused.Item2)
					y = listBox.ItemHeight * focused.Item1 + listBox.ItemHeight / 2;
				else
					y = listBox.ItemHeight * focused.Item1;
				if (y == 0)
					y = UIUtils.FocusedItemMarkBounds.Height / 2;
				y -= listBox.TopIndex * listBox.ItemHeight;
				UIUtils.DrawFocusedItemMark(e.Graphics, metrics.FocusedMessageMarkX, y);
			}
		}

		private void listBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				presenter.OnEnterKeyPressed();
			else if (e.KeyCode == Keys.C && e.Control)
				presenter.OnCopyShortcutPressed();
			else if (e.KeyCode == Keys.Insert && e.Control)
				presenter.OnCopyShortcutPressed();
			else if (e.KeyCode == Keys.Delete)
				presenter.OnDeleteButtonPressed();
			else if (e.KeyCode == Keys.A && e.Control)
				presenter.OnSelectAllShortcutPressed();
		}

		private void listBox1_MouseDown(object sender, MouseEventArgs e)
		{
			bool leftClick = e.Button == MouseButtons.Left;
			bool rightClick = e.Button == MouseButtons.Right;
			int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false, enabledOnly: leftClick);
			if (linkUnderMouse != null)
			{
				if (leftClick)
				{
					presenter.OnBookmarkLeftClicked(Get(linkUnderMouse.Value));
				}
				else if (rightClick)
				{
					listBox.SelectedIndex = linkUnderMouse.Value;
				}
			}
		}

		private void listBox1_MouseMove(object sender, MouseEventArgs e)
		{
			int? linkUnderMouse = GetLinkFromPoint(e.X, e.Y, false, true);
			listBox.Cursor = linkUnderMouse.HasValue ? Cursors.Hand : Cursors.Default;
		}

		IEnumerable<BookmarkItem> EnumItems()
		{
			return Enumerable.Range(0, listBox.Items.Count).Select(GetItem);
		}

		BookmarkItem GetItem(int index)
		{
			if (index >= 0 && index < listBox.Items.Count)
				return listBox.Items[index] as BookmarkItem;
			return null;
		}

		IBookmark Get(int index)
		{
			var item = GetItem(index);
			if (item != null)
				return item.Bookmark;
			return null;
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnMenuItemClicked(ContextMenuItem.Delete);
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnMenuItemClicked(ContextMenuItem.Copy);
		}

		private void copyWithTimeDeltasToolStripMenuItem_Click(object sender, EventArgs e)
		{
			presenter.OnMenuItemClicked(ContextMenuItem.CopyWithDeltas);
		}

		private void contextMenu_Opening(object sender, CancelEventArgs e)
		{
			ContextMenuItem items = presenter.OnContextMenu();
			if (items == ContextMenuItem.None)
			{
				e.Cancel = true;
				return;
			}
			deleteToolStripMenuItem.Visible = (items & ContextMenuItem.Delete) != 0;
			copyToolStripMenuItem.Visible = (items & ContextMenuItem.Copy) != 0;
			copyWithTimeDeltasToolStripMenuItem.Visible = (items & ContextMenuItem.CopyWithDeltas) != 0;
		}

		private void listBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (isUpdating)
				return;
			presenter.OnSelectionChanged();
		}

		class BookmarkItem
		{
			readonly public IBookmark Bookmark;
			readonly public string Delta, AltDelta;
			readonly public bool IsEnabled;

			public BookmarkItem(IBookmark bookmark, string delta, string altDelta, bool isEnabled)
			{
				Bookmark = bookmark;
				Delta = delta;
				AltDelta = altDelta;
				IsEnabled = isEnabled;
			}

			public override string ToString()
			{
				return Bookmark.ToString();
			}
		};

		Metrics GetMetrics()
		{
			if (metrics == null)
				metrics = CreateMetrics();
			return metrics;
		}

		private IViewEvents presenter;
		private IPresentationDataAccess presentationDataAccess;
		private Font timeDeltaDisplayFont;
		private Font linkDisplayFont;
		private StringFormat displayStringFormat;
		private Metrics metrics;
		private Brush selectedBkBrush = new SolidBrush(Color.FromArgb(197, 206, 231));
		private int lastSelectedCount = -1;
		private bool isUpdating;
	}

}
