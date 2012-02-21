using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using LogJoint.RegularExpressions;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters.LogViewer;
using System.Linq;

namespace LogJoint.UI
{
	public partial class LogViewerControl : Control, IView
	{
		public event EventHandler ManualRefresh;

		public bool ShowTime
		{
			get
			{
				return drawContext.ShowTime;
			}
			set
			{
				if (drawContext.ShowTime == value)
					return;
				drawContext.ShowTime = value;
				Invalidate();
			}
		}

		public bool ShowMilliseconds 
		{ 
			get
			{
				return drawContext.ShowMilliseconds;
			}
			set
			{
				if (drawContext.ShowMilliseconds == value)
					return;
				drawContext.ShowMilliseconds = value;
				UpdateTimeAreaSize();
				if (drawContext.ShowTime)
				{
					Invalidate();
				}
			}
		}


		public LogViewerControl()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			base.BackColor = Color.White;
			base.TabStop = true;
			base.Enabled = true;

			drawContext.Font = new Font("Courier New", 10);

			var prototypeStringFormat = StringFormat.GenericDefault;
			drawContext.MultilineTextFormat = (StringFormat)prototypeStringFormat.Clone();
			drawContext.MultilineTextFormat.FormatFlags |= StringFormatFlags.LineLimit;
			drawContext.SinglelineTextFormat = (StringFormat)prototypeStringFormat.Clone();
			drawContext.SinglelineTextFormat.SetTabStops(0, new float[] {20});


			drawContext.OutlineMarkupPen = new Pen(Color.Gray, 1);
			drawContext.SelectedOutlineMarkupPen = new Pen(Color.White, 1);

			drawContext.InfoMessagesBrush = SystemBrushes.ControlText;
			drawContext.SelectedTextBrush = SystemBrushes.HighlightText;
			drawContext.SelectedFocuslessTextBrush = SystemBrushes.ControlText;
			drawContext.CommentsBrush = SystemBrushes.GrayText;

			drawContext.DefaultBackgroundBrush = SystemBrushes.Window;
			drawContext.SelectedBkBrush = new SolidBrush(Color.FromArgb(167, 176, 201));
			drawContext.SelectedFocuslessBkBrush = Brushes.Gray;
			
			drawContext.ErrorIcon = errPictureBox.Image;
			drawContext.WarnIcon = warnPictureBox.Image;
			drawContext.BookmarkIcon = bookmarkPictureBox.Image;
			drawContext.SmallBookmarkIcon = smallBookmarkPictureBox.Image;

			drawContext.HighlightPen = new Pen(Color.Red, 3);
			drawContext.HighlightPen.LineJoin = LineJoin.Round;

			drawContext.TimeSeparatorLine = new Pen(Color.Gray, 1);

			drawContext.HighlightBrush = Brushes.Cyan;

			drawContext.InplaceHightlightBackground = Brushes.LightSalmon;

			using (Graphics tmp = Graphics.FromHwnd(IntPtr.Zero))
			{
				int count = 8*1024;
				drawContext.CharSize = tmp.MeasureString(new string('0', count), drawContext.Font);
				drawContext.CharWidthDblPrecision = (double)drawContext.CharSize.Width / (double)count;
				drawContext.CharSize.Width /= (float)count;
			}

			drawContext.MessageHeight = (int)Math.Floor(drawContext.CharSize.Height);
			UpdateTimeAreaSize();
			drawContext.ShowTime = false;
		}

		public void SetPresenter(Presenter presenter)
		{
			this.presenter = presenter;

			this.tracer = presenter.Tracer;
		}

		public Presenter Presenter { get { return presenter; } }

		void UpdateTimeAreaSize()
		{
			string testStr = MessageBase.FormatTime(
				new DateTime(2011, 11, 11, 11, 11, 11, 111), 
				drawContext.ShowMilliseconds
			);
			drawContext.TimeAreaSize = (int)Math.Floor(
				drawContext.CharSize.Width * (float)testStr.Length
			) + 5;
		}

		static class Native
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct SCROLLINFO
			{
				public int cbSize;
				public SIF fMask;
				public int nMin;
				public int nMax;
				public int nPage;
				public int nPos;
				public int nTrackPos;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int left;
				public int top;
				public int right;
				public int bottom;
				public RECT(Rectangle r)
				{
					left = r.Left;
					top = r.Top;
					right = r.Right;
					bottom = r.Bottom;
				}
				public Rectangle ToRectangle()
				{
					return new Rectangle(left, top, right - left, bottom - top);
				}
			}

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern int SetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si, bool redraw);

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern bool GetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si);

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
			public static extern int ScrollWindowEx(
				HandleRef hWnd, 
				int nXAmount, int nYAmount,
				ref RECT rectScrollRegion,
				ref RECT rectClip, 
				HandleRef hrgnUpdate, 
				ref RECT prcUpdate, 
				int flags);

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
			public static extern int RedrawWindow(
				HandleRef hWnd,
				IntPtr rectClip,
				IntPtr hrgnUpdate,
				UInt32 flags
			);

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public static extern bool PostMessage(HandleRef hwnd, 
				int msg, IntPtr wparam, IntPtr lparam);

			public const int WM_USER = 0x0400;

			public static int LOWORD(int n)
			{
				return (n & 0xffff);
			}

			public static int LOWORD(IntPtr n)
			{
				return LOWORD((int)((long)n));
			}

			public enum SB: int
			{
				LINEUP = 0,
				LINELEFT = 0,
				LINEDOWN = 1,
				LINERIGHT = 1,
				PAGEUP = 2,
				PAGELEFT = 2,
				PAGEDOWN = 3,
				PAGERIGHT = 3,
				THUMBPOSITION = 4,
				THUMBTRACK = 5,
				TOP = 6,
				LEFT = 6,
				BOTTOM = 7,
				RIGHT = 7,
				ENDSCROLL = 8,

				HORZ = 0,
				VERT = 1,
				BOTH = 3,
			}

			public enum SIF: uint
			{
				RANGE = 0x0001,
				PAGE = 0x0002,
				POS = 0x0004,
				DISABLENOSCROLL = 0x0008,
				TRACKPOS = 0x0010,
				ALL = (RANGE | PAGE | POS | TRACKPOS),
			}
		};

		public void HScrollToSelectedText()
		{
			if (selection.IsEmpty)
				return;

			int pixelThatMustBeVisible = (int)(selection.Begin.LineCharIndex * drawContext.CharSize.Width);			

			if (pixelThatMustBeVisible < sb.scrollPos.X
			 || pixelThatMustBeVisible >= sb.scrollPos.X + ClientSize.Width - SystemInformation.VerticalScrollBarWidth)
			{
				SetScrollPos(new Point(pixelThatMustBeVisible, sb.scrollPos.Y));
			}
		}

		public void UpdateScrollSizeToMatchVisibleCount()
		{
			SetScrollSize(new Size(10, visibleCount * drawContext.MessageHeight), true, false);
		}

		struct VisibleMessages
		{
			public int begin;
			public int end;
			public int fullyVisibleEnd;
		};

		VisibleMessages GetVisibleMessages(Rectangle viewRect)
		{
			VisibleMessages rv;
			
			viewRect.Offset(0, ScrollPos.Y);

			rv.begin = viewRect.Y / drawContext.MessageHeight;
			rv.fullyVisibleEnd = rv.end = viewRect.Bottom / drawContext.MessageHeight;

			if ((viewRect.Bottom % drawContext.MessageHeight) != 0)
				++rv.end;
			
			rv.begin = Math.Min(visibleCount, rv.begin);
			rv.end = Math.Min(visibleCount, rv.end);
			rv.fullyVisibleEnd = Math.Min(visibleCount, rv.fullyVisibleEnd);

			return rv;
		}

		public IEnumerable<Presenter.DisplayLine> GetVisibleMessagesIterator()
		{
			return GetVisibleMessagesIterator(ClientRectangle);
		}

		IEnumerable<Presenter.DisplayLine> GetVisibleMessagesIterator(Rectangle viewRect)
		{
			if (presenter == null)
				return Enumerable.Empty<Presenter.DisplayLine>();
			VisibleMessages vl = GetVisibleMessages(viewRect);
			return presenter.GetDisplayLines(vl.begin, vl.end);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			Rectangle clientRectangle = base.ClientRectangle;
			int p = this.ScrollPos.Y - e.Delta;
			SetScrollPos(new Point(sb.scrollPos.X, p));
			if (e is HandledMouseEventArgs)
			{
				((HandledMouseEventArgs)e).Handled = true;
			}
			base.OnMouseWheel(e);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			drawContext.ScrollPos = this.ScrollPos;
			foreach (var i in GetVisibleMessagesIterator(ClientRectangle))
			{
				drawContext.MessageIdx = i.DisplayLineIndex;
				MessageBase l = i.Message;
				var m = DrawingUtils.GetMetrics(l, drawContext);
				if (!m.MessageRect.Contains(e.X, e.Y))
					continue;
				if (m.OulineBox.Contains(e.X, e.Y))
					continue;
				presenter.PerformDefaultFocusedMessageAction();
				break;
			}
			base.OnMouseDoubleClick(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			InvalidateMessagesArea();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			InvalidateMessagesArea();
			base.OnLostFocus(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.Focus();

			using (var g = this.CreateGraphics())
			{
				drawContext.ScrollPos = this.ScrollPos;
				drawContext.Canvas = g;
				foreach (var i in GetVisibleMessagesIterator(ClientRectangle))
				{
					drawContext.MessageIdx = i.DisplayLineIndex;
					DrawingUtils.Metrics mtx = DrawingUtils.GetMetrics(i.Message, drawContext);

					// if used clicked line's outline box (collapse/expand cross)
					if (mtx.OulineBox.Contains(e.X, e.Y))
						if (presenter.OulineBoxClicked(i.Message, ModifierKeys == Keys.Control))
							break;

					// if user clicked line area
					if (mtx.MessageRect.Contains(e.X, e.Y))
					{
						var hitTester = new HitTestingVisitor(drawContext, mtx, e.Location.X, i.TextLineIndex);
						i.Message.Visit(hitTester);
						presenter.MessageRectClicked(CursorPosition.FromDisplayLine(i, hitTester.LineTextPosition),
							e.Button == MouseButtons.Right, Control.ModifierKeys == Keys.Shift, e.Location);
						break;
					}
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			drawContext.ScrollPos = this.ScrollPos;

			if (e.Button == MouseButtons.Left)
			{
				using (var g = this.CreateGraphics())
				{
					drawContext.Canvas = g;
					foreach (var i in GetVisibleMessagesIterator(ClientRectangle))
					{
						drawContext.MessageIdx = i.DisplayLineIndex;
						DrawingUtils.Metrics mtx = DrawingUtils.GetMetrics(i.Message, drawContext);

						if (e.Y >= mtx.MessageRect.Top && e.Y < mtx.MessageRect.Bottom)
						{
							var hitTester = new HitTestingVisitor(drawContext, mtx, e.Location.X, i.TextLineIndex);
							i.Message.Visit(hitTester);
							presenter.MessageRectClicked(CursorPosition.FromDisplayLine(i, hitTester.LineTextPosition), false, true, e.Location);
							break;
						}
					}
				}

			}

			Cursor newCursor = e.X >= drawContext.GetTextOffset(0).X ? Cursors.IBeam : Cursors.Arrow;
			if (Cursor != newCursor)
				Cursor = newCursor;

			base.OnMouseMove(e);
		}

		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			if (e.Modifiers == Keys.Shift && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
			{
				e.IsInputKey = true;
			}
			base.OnPreviewKeyDown(e);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Up || keyData == Keys.Down 
					|| keyData == Keys.Left || keyData == Keys.Right)
				return true;
			return base.IsInputKey(keyData);
		}

		public void PopupContextMenu(object contextMenuPopupData)
		{
			Point pt;
			if (contextMenuPopupData is Point)
				pt = (Point)contextMenuPopupData;
			else
				pt = new Point();
			DoContextMenu(pt.X, pt.Y);
		}

		void DoContextMenu(int x, int y)
		{
			contextMenuStrip1.Show(this, x, y);
		}

		public void SetClipboard(string text)
		{
			try
			{
				Clipboard.SetText(text);
			}
			catch (Exception)
			{
				MessageBox.Show("Failed to copy data to the clipboard");
			}
		}

		void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (e.ClickedItem == this.copyMenuItem)
				presenter.CopySelectionToClipboard();
			else if (e.ClickedItem == this.collapseMenuItem)
				presenter.DoExpandCollapse(selection.Message, false, new bool?());
			else if (e.ClickedItem == this.recursiveCollapseMenuItem)
				presenter.DoExpandCollapse(selection.Message, true, new bool?());
			else if (e.ClickedItem == this.gotoParentFrameMenuItem)
				presenter.GoToParentFrame();
			else if (e.ClickedItem == this.gotoEndOfFrameMenuItem)
				presenter.GoToEndOfFrame();
			else if (e.ClickedItem == this.showTimeMenuItem)
				ShowTime = (showTimeMenuItem.Checked = !showTimeMenuItem.Checked);
			else if (e.ClickedItem == this.defaultActionMenuItem)
				presenter.PerformDefaultFocusedMessageAction();
			else if (e.ClickedItem == this.toggleBmkStripMenuItem)
				presenter.ToggleBookmark(selection.Message);
			else if (e.ClickedItem == this.gotoNextMessageInTheThreadMenuItem)
				presenter.GoToNextMessageInThread();
			else if (e.ClickedItem == this.gotoPrevMessageInTheThreadMenuItem)
				presenter.GoToPrevMessageInThread();			
		}

		protected override void OnKeyDown(KeyEventArgs kevent)
		{
			Keys k = kevent.KeyCode;
			bool ctrl = kevent.Modifiers == Keys.Control;
			bool alt = kevent.Modifiers == Keys.Alt;
			bool shift = kevent.Modifiers == Keys.Shift;
			Presenter.SelectionFlag preserveSelectionFlag = shift ? Presenter.SelectionFlag.PreserveSelectionEnd : Presenter.SelectionFlag.None;

			if (k == Keys.F5)
			{
				OnRefresh();
				return;
			}

			CursorPosition cur = selection.Begin;

			if (selection.Message != null)
			{
				if (k == Keys.Up)
					if (ctrl)
						presenter.GoToParentFrame();
					else if (alt)
						presenter.GoToPrevMessageInThread();
					else
						presenter.MoveSelection(selection.DisplayPosition - 1, Presenter.MoveSelectionFlag.ForwardShiftingMode, preserveSelectionFlag);
				else if (k == Keys.Down)
					if (ctrl)
						presenter.GoToEndOfFrame();
					else if (alt)
						presenter.GoToNextMessageInThread();
					else
						presenter.MoveSelection(selection.DisplayPosition + 1, Presenter.MoveSelectionFlag.BackwardShiftingMode, preserveSelectionFlag);
				else if (k == Keys.PageUp)
					presenter.MoveSelection(selection.DisplayPosition - Height / drawContext.MessageHeight, Presenter.MoveSelectionFlag.ForwardShiftingMode,
							preserveSelectionFlag);
				else if (k == Keys.PageDown)
					presenter.MoveSelection(selection.DisplayPosition + Height / drawContext.MessageHeight, Presenter.MoveSelectionFlag.BackwardShiftingMode,
							preserveSelectionFlag);
				else if (k == Keys.Left || k == Keys.Right)
				{
					var left = k == Keys.Left;
					if (!presenter.DoExpandCollapse(selection.Message, ctrl, left))
					{
						presenter.SetSelection(cur.DisplayIndex, preserveSelectionFlag, cur.LineCharIndex + (left ? -1 : +1));
						if (selection.Begin.LineCharIndex == cur.LineCharIndex)
						{
							presenter.MoveSelection(
								selection.DisplayPosition + (left ? -1 : +1),
								left ? Presenter.MoveSelectionFlag.ForwardShiftingMode : Presenter.MoveSelectionFlag.BackwardShiftingMode,
								preserveSelectionFlag | (left ? Presenter.SelectionFlag.SelectEndOfLine : Presenter.SelectionFlag.SelectBeginningOfLine)
							);
						}
						//int delta = 20;
						//int x = sb.scrollPos.X + (k == Keys.Left ? -delta : delta);
						//SetScrollPos(new Point(x, sb.scrollPos.Y));
						//presenter.InvalidateFocusedMessage();
					}
				}
				else if (k == Keys.Apps)
					DoContextMenu(0, (selection.DisplayPosition + 1) * drawContext.MessageHeight - 1 - ScrollPos.Y);
				else if (k == Keys.Enter)
					presenter.PerformDefaultFocusedMessageAction();
			}
			if (k == Keys.C && ctrl)
			{
				presenter.CopySelectionToClipboard();
			}
			if (k == Keys.Home)
			{
				presenter.SetSelection(cur.DisplayIndex, preserveSelectionFlag | Presenter.SelectionFlag.SelectBeginningOfLine);
				if (ctrl && !presenter.GetShiftPermissions().AllowUp)
				{
					presenter.ShiftHome();
					presenter.SelectFirstMessage();
				}
			}
			else if (k == Keys.End)
			{
				if (ctrl && !presenter.GetShiftPermissions().AllowDown)
				{
					presenter.ShiftToEnd();
					presenter.SelectLastMessage();
				}
				presenter.SetSelection(cur.DisplayIndex, preserveSelectionFlag | Presenter.SelectionFlag.SelectEndOfLine);
			}
			base.OnKeyDown(kevent);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing. All painting is in OnPaint.
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			DrawContext dc = drawContext;

			dc.Canvas = pe.Graphics;
			dc.ClientRect = ClientRectangle;
			dc.ScrollPos = this.ScrollPos;
			dc.ControlFocused = this.Focused;
			dc.NormalizedSelection = selection.Normalize();

			dc.Canvas.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

			// Area covered by visible lines
			Rectangle messagesArea = new Rectangle(FixedMetrics.CollapseBoxesAreaSize, 0, 0, 0);

			bool drawFocus = this.Focused && selection.Message != null;

			// Fill outline area with default brush
			Rectangle outlineArea = new Rectangle(0, 0, FixedMetrics.CollapseBoxesAreaSize, Height);
			dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, outlineArea);

			DrawOutlineVisitor drawOutlineVisitor = new DrawOutlineVisitor();
			drawOutlineVisitor.drawContext = dc;

			foreach (var il in GetVisibleMessagesIterator(pe.ClipRectangle))
			{
				MessageBase l = il.Message;
				dc.MessageIdx = il.DisplayLineIndex;
				dc.TextLineIdx = il.TextLineIndex;
				dc.MessageFocused = drawFocus && (il.Message == selection.Begin.Message);
				drawOutlineVisitor.metrics = DrawingUtils.GetMetrics(l, dc);
				l.Visit(drawOutlineVisitor);
			}

			int maxRight = 0;

			GraphicsState state = dc.Canvas.Save();
			try
			{
				dc.Canvas.ExcludeClip(outlineArea);

				var drawBackgroundVisitor = new DrawBackgroundVisitor();
				drawBackgroundVisitor.ctx = dc;

				var drawingVisitor = new DrawingVisitor();
				drawingVisitor.ctx = dc;
				drawingVisitor.inplaceHighlightHandler = presenter != null ? presenter.InplaceHighlightHandler : null;


				// Get visible lines and draw them
				foreach (var il in GetVisibleMessagesIterator(pe.ClipRectangle))
				{
					MessageBase l = il.Message;

					// Prepare draw context
					dc.MessageIdx = il.DisplayLineIndex;
					dc.TextLineIdx = il.TextLineIndex;
					dc.MessageFocused = drawFocus && (il.DisplayLineIndex == selection.DisplayPosition);

					DrawingUtils.Metrics m = DrawingUtils.GetMetrics(l, dc);
					drawingVisitor.m = m;
					drawBackgroundVisitor.m = m;

					// Draw line's background
					l.Visit(drawBackgroundVisitor);

					// Draw the line
					l.Visit(drawingVisitor);

					// Add bounds of current line to linesRect
					dc.Canvas.ExcludeClip(m.MessageRect);

					maxRight = Math.Max(maxRight, m.OffsetTextRect.Right);
				}

				// Fill remaining area with default background
				dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, dc.ClientRect);
			}
			finally
			{
				dc.Canvas.Restore(state);
			}

			DrawTime(dc);

			DrawCursor(dc);

			UpdateScrollSize(dc, maxRight);

			base.OnPaint(pe);
		}

		private void UpdateScrollSize(DrawContext dc, int maxRight)
		{
			maxRight += dc.ScrollPos.X;
			if (maxRight > sb.scrollSize.Width)
			{
				SetScrollSize(new Size(maxRight, sb.scrollSize.Height), false, true);
			}
		}

		private static void DrawTime(DrawContext dc)
		{
			if (dc.ShowTime)
			{
				float x = FixedMetrics.CollapseBoxesAreaSize + dc.TimeAreaSize - dc.ScrollPos.X - 2;
				if (x > FixedMetrics.CollapseBoxesAreaSize)
					dc.Canvas.DrawLine(dc.TimeSeparatorLine, x, 0, x, dc.ClientRect.Height);
			}
		}

		private void DrawCursor(DrawContext dc)
		{
			CursorPosition cur = selection.Begin;
			if (cur.Message == null)
				return;
			dc.MessageIdx = cur.DisplayIndex;
			var visitor = new DrawCursorVisitor() { ctx = dc, m = DrawingUtils.GetMetrics(selection.Message, dc), pos = cur };
			cur.Message.Visit(visitor);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize(e);
		}

		void InvalidateMessagesArea()
		{
			Rectangle r = ClientRectangle;
			r.X += FixedMetrics.CollapseBoxesAreaSize;
			r.Width -= FixedMetrics.CollapseBoxesAreaSize;
			Invalidate(r);
		}

		public void DisplayEverythingFilteredOutMessage(bool displayOrHide)
		{
			if (everythingFilteredOutMessage == null)
			{
				everythingFilteredOutMessage = new EverythingFilteredOutMessage();
				everythingFilteredOutMessage.Visible = displayOrHide;
				Controls.Add(everythingFilteredOutMessage);
				everythingFilteredOutMessage.Dock = DockStyle.Fill;
				everythingFilteredOutMessage.FiltersLinkLabel.Click += (s, e) => presenter.OnShowFiltersClicked();
				everythingFilteredOutMessage.SearchUpLinkLabel.Click +=
					(s, e) => presenter.Search(new SearchOptions() { CoreOptions = new Search.Options() { ReverseSearch = true }, HighlightResult = false });
				everythingFilteredOutMessage.SearchDownLinkLabel.Click +=
					(s, e) => presenter.Search(new SearchOptions() { CoreOptions = new Search.Options() { ReverseSearch = false }, HighlightResult = false });
			}
			else
			{
				everythingFilteredOutMessage.Visible = displayOrHide;
			}
		}

		public void DisplayNothingLoadedMessage(string messageToDisplayOrNull)
		{
			if (string.IsNullOrWhiteSpace(messageToDisplayOrNull))
				messageToDisplayOrNull = null;
			if (emptyMessagesCollectionMessage == null)
			{
				emptyMessagesCollectionMessage = new EmptyMessagesCollectionMessage();
				emptyMessagesCollectionMessage.Visible = messageToDisplayOrNull != null;
				Controls.Add(emptyMessagesCollectionMessage);
				emptyMessagesCollectionMessage.Dock = DockStyle.Fill;
			}
			else
			{
				emptyMessagesCollectionMessage.Visible = messageToDisplayOrNull != null;
			}
			if (messageToDisplayOrNull != null)
				emptyMessagesCollectionMessage.SetMessage(messageToDisplayOrNull);
		}

		public void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage)
		{
			if (sb.userIsScrolling)
			{
				return;
			}

			int? newScrollPos = null;

			VisibleMessages vl = GetVisibleMessages(ClientRectangle);

			int extra = showExtraLinesAroundMessage ? 2 : 0;

			if (messageDisplayPosition < vl.begin + extra)
				newScrollPos = messageDisplayPosition - extra;
			else if (messageDisplayPosition >= vl.fullyVisibleEnd - extra)
				newScrollPos = messageDisplayPosition + 2 - (vl.fullyVisibleEnd - vl.begin) + extra;

			if (newScrollPos.HasValue)
				SetScrollPos(new Point(sb.scrollPos.X, newScrollPos.Value * drawContext.MessageHeight));
		}

		public void UpdateStarted()
		{
			if (prenterUpdate != null)
				return;

			prenterUpdate = new PrenterUpdate()
			{
				FocusedBeforeUpdate = presenter.FocusedMessage,
				RelativeForcusedScrollPositionBeforeUpdate = selection.DisplayPosition * drawContext.MessageHeight - sb.scrollPos.Y
			};
		}

		public void UpdateFinished()
		{
			if (prenterUpdate == null)
				return;
			try
			{
				if (prenterUpdate.FocusedBeforeUpdate != null
				 && presenter.FocusedMessage != null)
				{
					SetScrollPos(new Point(sb.scrollPos.X,
						selection.DisplayPosition * drawContext.MessageHeight - prenterUpdate.RelativeForcusedScrollPositionBeforeUpdate));
				}
			}
			finally
			{
				prenterUpdate = null;
			}
		}

		public void InvalidateMessage(MessageBase msg, int displayPosition)
		{
			drawContext.MessageIdx = displayPosition;
			Rectangle r = DrawingUtils.GetMetrics(msg, drawContext).MessageRect;
			this.Invalidate(r);
		}

		private Point ScrollPos
		{
			get
			{
				return new Point(GetScrollInfo(Native.SB.HORZ).nPos,
					GetScrollInfo(Native.SB.VERT).nPos);
			}
		}

		void SetScrollPos(Point pos)
		{
			if (pos.Y > sb.scrollSize.Height)
				pos.Y = sb.scrollSize.Height;
			else if (pos.Y < 0)
				pos.Y = 0;

			int maxPosX = Math.Max(0, sb.scrollSize.Width - ClientSize.Width);

			if (pos.X > maxPosX)
				pos.X = maxPosX;
			else if (pos.X < 0)
				pos.X = 0;

			int xBefore = GetScrollInfo(Native.SB.HORZ).nPos;
			int yBefore = GetScrollInfo(Native.SB.VERT).nPos;

			bool vRedraw = pos.Y != sb.scrollPos.Y;
			bool hRedraw = pos.X != sb.scrollPos.X;
			sb.scrollPos = pos;
			UpdateScrollBars(vRedraw, hRedraw);

			int xDelta = xBefore - GetScrollInfo(Native.SB.HORZ).nPos;
			int yDelta = yBefore - GetScrollInfo(Native.SB.VERT).nPos;

			if (xDelta == 0 && yDelta == 0)
			{
			}
			else if (xDelta != 0 && yDelta != 0)
			{
				Invalidate();
			}
			else
			{
				Rectangle r = ClientRectangle;
				if (xDelta != 0)
				{
					r.X += FixedMetrics.CollapseBoxesAreaSize;
					r.Width -= FixedMetrics.CollapseBoxesAreaSize;
				}
				Native.RECT scroll = new Native.RECT(r);
				Native.RECT clip = scroll;
				Native.RECT update = scroll;
				HandleRef hrgnUpdate = new HandleRef(null, IntPtr.Zero);
				Native.ScrollWindowEx(
					new HandleRef(this, base.Handle),
					xDelta, yDelta,
					ref scroll, ref clip, hrgnUpdate, ref update, 2);
			}
		}

		void SetScrollSize(Size sz, bool vRedraw, bool hRedraw)
		{
			sb.scrollSize = sz;
			UpdateScrollBars(vRedraw, hRedraw);
		}

		void UpdateScrollBars(bool vRedraw, bool hRedraw)
		{
			InternalUpdateScrollBars(vRedraw, hRedraw, false);
		}

		void InternalUpdateScrollBars(bool vRedraw, bool hRedraw, bool redrawNow)
		{
			if (this.IsHandleCreated && Visible)
			{
				HandleRef handle = new HandleRef(this, this.Handle);

				Native.SCROLLINFO v = new Native.SCROLLINFO();
				v.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
				v.fMask = Native.SIF.ALL;
				v.nMin = 0;
				v.nMax = sb.scrollSize.Height;
				v.nPage = ClientSize.Height;
				v.nPos = sb.scrollPos.Y;
				v.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.VERT, ref v, redrawNow && vRedraw);

				Native.SCROLLINFO h = new Native.SCROLLINFO();
				h.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
				h.fMask = Native.SIF.ALL;
				h.nMin = 0;
				h.nMax = sb.scrollSize.Width;
				h.nPage = ClientSize.Width;
				h.nPos = sb.scrollPos.X;
				h.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.HORZ, ref h, redrawNow && hRedraw);

				if (!redrawNow)
				{
					sb.vRedraw |= vRedraw;
					sb.hRedraw |= hRedraw;
					if (!sb.repaintPosted)
					{
						Native.PostMessage(handle, ScrollBarsInfo.WM_REPAINTSCROLLBARS, IntPtr.Zero, IntPtr.Zero);
						sb.repaintPosted = true;
					}
				}
			}
		}

		private void WMRepaintScrollBars()
		{
			InternalUpdateScrollBars(sb.vRedraw, sb.hRedraw, true);
			sb.repaintPosted = false;
			sb.hRedraw = false;
			sb.vRedraw = false;
		}

		private void WmHScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, sb.scrollPos.X, sb.scrollSize.Width, Native.SB.HORZ);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(ret, sb.scrollPos.Y));
			}
			presenter.InvalidateFocusedMessage();
		}

		private Native.SCROLLINFO GetScrollInfo(Native.SB sb)
		{
			Native.SCROLLINFO si = new Native.SCROLLINFO();
			si.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
			si.fMask = Native.SIF.ALL;
			Native.GetScrollInfo(new HandleRef(this, base.Handle), sb, ref si);
			return si;
		}

		int DoWmScroll(ref System.Windows.Forms.Message m,
			int num, int maximum, Native.SB bar)
		{
			if (m.LParam != IntPtr.Zero)
			{
				base.WndProc(ref m);
				return -1;
			}
			else
			{
				int smallChange = 50;
				int largeChange = 200;

				Native.SB sbEvt = (Native.SB)Native.LOWORD(m.WParam);
				switch (sbEvt)
				{
					case Native.SB.LINEUP:
						num -= smallChange;
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.LINEDOWN:
						num += smallChange;
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.PAGEUP:
						num -= largeChange;
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.PAGEDOWN:
						num += largeChange;
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.THUMBTRACK:
						sb.userIsScrolling = true;
						num = this.GetScrollInfo(bar).nTrackPos;
						break;

					case Native.SB.THUMBPOSITION:
						num = this.GetScrollInfo(bar).nTrackPos;
						break;

					case Native.SB.TOP:
						num = 0;
						break;

					case Native.SB.BOTTOM:
						num = maximum;
						break;

					case Native.SB.ENDSCROLL:
						sb.userIsScrolling = false;
						break;
				}

				return num;
			}
		}

		private void WmVScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, sb.scrollPos.Y, sb.scrollSize.Height, Native.SB.VERT);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(sb.scrollPos.X, ret));
			}
		}

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			switch (m.Msg)
			{
				case 0x114:
					this.WmHScroll(ref m);
					return;

				case 0x115:
					this.WmVScroll(ref m);
					return;

				case ScrollBarsInfo.WM_REPAINTSCROLLBARS:
					this.WMRepaintScrollBars();
					return;
			}
			base.WndProc(ref m);
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			showTimeMenuItem.Checked = ShowTime;
			toggleBmkStripMenuItem.Visible = presenter.BookmarksAvailable;

			string defaultAction = presenter.DefaultFocusedMessageActionCaption;
			defaultActionMenuItem.Visible = !string.IsNullOrEmpty(defaultAction);
			defaultActionMenuItem.Text = defaultAction;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			this.UpdateScrollBars(true, true);
			base.OnLayout(levent);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.Style |= 0x100000; // horz scroll
				createParams.Style |= 0x200000; // vert scroll
				return createParams;
			}
		}

		protected virtual void OnRefresh()
		{
			if (ManualRefresh != null)
				ManualRefresh(this, EventArgs.Empty);
		}

		Presenter presenter;
		
		LJTraceSource tracer = LJTraceSource.EmptyTracer;

		struct ScrollBarsInfo
		{
			public const int WM_REPAINTSCROLLBARS = Native.WM_USER + 98;
			public Point scrollPos;
			public Size scrollSize;
			public bool vRedraw;
			public bool hRedraw;
			public bool repaintPosted;
			public bool userIsScrolling;
		};
		ScrollBarsInfo sb;

		class PrenterUpdate
		{
			public MessageBase FocusedBeforeUpdate;
			public int RelativeForcusedScrollPositionBeforeUpdate;
		};
		PrenterUpdate prenterUpdate;

		DrawContext drawContext = new DrawContext();
		int visibleCount { get { return presenter != null ? presenter.DisplayMessages.Count : 0; } }
		SelectionInfo selection { get { return presenter != null ? presenter.Selection : new SelectionInfo(); } }
		EverythingFilteredOutMessage everythingFilteredOutMessage;
		EmptyMessagesCollectionMessage emptyMessagesCollectionMessage;
	}
}
