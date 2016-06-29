using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using LogJoint.UI.Presenters.LogViewer;
using System.Linq;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI
{
	public partial class LogViewerControl : Control, IView, IViewFonts
	{
		public LogViewerControl()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			base.BackColor = Color.White;
			base.TabStop = true;
			base.Enabled = true;

			bufferedGraphicsContext = new BufferedGraphicsContext() { MaximumBuffer = new Size(5000, 4000) };

			drawContext.CollapseBoxesAreaSize = UIUtils.Dpi.Scale(drawContext.CollapseBoxesAreaSize);
			drawContext.OutlineBoxSize = UIUtils.Dpi.Scale(drawContext.OutlineBoxSize);
			drawContext.OutlineCrossSize = UIUtils.Dpi.Scale(drawContext.OutlineCrossSize);
			drawContext.LevelOffset = UIUtils.Dpi.Scale(drawContext.LevelOffset);
			drawContext.DpiScale = UIUtils.Dpi.Scale(1f);

			var prototypeStringFormat = (StringFormat)StringFormat.GenericDefault.Clone();
			prototypeStringFormat.SetTabStops(0, new float[] { 20 });
			prototypeStringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
			drawContext.TextFormat = new LJD.StringFormat(prototypeStringFormat);


			drawContext.OutlineMarkupPen = new LJD.Pen(Color.Gray, 1);
			drawContext.SelectedOutlineMarkupPen = new LJD.Pen(Color.White, 1);

			drawContext.InfoMessagesBrush = new LJD.Brush(SystemColors.ControlText);
			drawContext.SelectedTextBrush = new LJD.Brush(SystemColors.HighlightText);
			drawContext.SelectedFocuslessTextBrush = new LJD.Brush(SystemColors.ControlText);
			drawContext.CommentsBrush = new LJD.Brush(SystemColors.GrayText);

			drawContext.DefaultBackgroundBrush = new LJD.Brush(SystemColors.Window);
			drawContext.SelectedBkBrush = new LJD.Brush(Color.FromArgb(167, 176, 201));
			drawContext.SelectedFocuslessBkBrush = new LJD.Brush(Color.Gray);

			drawContext.FocusedMessageBkBrush = new LJD.Brush(Color.FromArgb(167 + 30, 176 + 30, 201 + 30));

			drawContext.ErrorIcon = new LJD.Image(Properties.Resources.ErrorLogSeverity);
			drawContext.WarnIcon = new LJD.Image(Properties.Resources.WarnLogSeverity);
			drawContext.BookmarkIcon = new LJD.Image(Properties.Resources.Bookmark);
			drawContext.FocusedMessageIcon = new LJD.Image(Properties.Resources.FocusedMsg);
			drawContext.FocusedMessageSlaveIcon = new LJD.Image(Properties.Resources.FocusedMsgSlave);

			drawContext.CursorPen = new LJD.Pen(Color.Black, 2);

			drawContext.TimeSeparatorLine = new LJD.Pen(Color.Gray, 1);

			drawContext.HighlightBrush = new LJD.Brush(Color.Cyan);

			int hightlightingAlpha = 170;
			drawContext.InplaceHightlightBackground1 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			drawContext.InplaceHightlightBackground2 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));

			rightCursor = new System.Windows.Forms.Cursor(
				System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("LogJoint.ui.LogViewerControl.cursor_r.cur"));

			drawContext.ViewWidth = this.ClientRectangle.Width;

			EnsureBackbufferIsUpToDate();

			cursorTimer.Tick += (s, e) =>
			{
				drawContext.CursorState = !drawContext.CursorState;
				if (viewEvents != null)
					viewEvents.OnCursorTimerTick();
			};

			animationTimer.Tick += (s, e) =>
			{
				if (drawContext.SlaveMessagePositionAnimationStep < 8)
				{
					drawContext.SlaveMessagePositionAnimationStep++;
				}
				else
				{
					animationTimer.Enabled = false;
					drawContext.SlaveMessagePositionAnimationStep = 0;
				}
				Invalidate();
			};

			menuItemsMap = MakeMenuItemsMap();
		}

		#region IView members

		void IView.UpdateFontDependentData(string fontName, LogFontSize fontSize)
		{
			if (drawContext.Font != null)
				drawContext.Font.Dispose();

			drawContext.Font = new LJD.Font(GetFontFamily(fontName).Name, ToFontEmSize(fontSize));

			using (var nativeGraphics = CreateGraphics())
			using (var tmp = new LJD.Graphics(nativeGraphics))
			{
				int count = 8 * 1024;
				drawContext.CharSize = tmp.MeasureString(new string('0', count), drawContext.Font);
				drawContext.CharWidthDblPrecision = (double)drawContext.CharSize.Width / (double)count;
				drawContext.CharSize.Width /= (float)count;
				drawContext.LineHeight = (int)Math.Floor(drawContext.CharSize.Height);
			}

			UpdateTimeAreaSize();
		}

		private static int ToFontEmSize(LogFontSize fontSize)
		{
			switch (fontSize)
			{
				case LogFontSize.SuperSmall: return 6;
				case LogFontSize.ExtraSmall: return 7;
				case LogFontSize.Small: return 8;
				case LogFontSize.Large1: return 10;
				case LogFontSize.Large2: return 11;
				case LogFontSize.Large3: return 14;
				case LogFontSize.Large4: return 16;
				case LogFontSize.Large5: return 18;
				default: return 9;
			}
		}

		void IView.SetViewEvents(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetPresentationDataAccess(IPresentationDataAccess presentationDataAccess)
		{
			this.presentationDataAccess = presentationDataAccess;
			this.drawContext.Presenter = presentationDataAccess;
		}

		void IView.SaveViewScrollState(SelectionInfo selection)
		{
			if (presenterUpdate != null)
				return;

			presenterUpdate = new PresenterUpdate()
			{
				FocusedBeforeUpdate = selection.Message,
				RelativeForcusedScrollPositionBeforeUpdate = selection.DisplayPosition * drawContext.LineHeight - scrollBarsInfo.scrollPos.Y
			};
		}

		void IView.RestoreViewScrollState(SelectionInfo selection)
		{
			if (presenterUpdate == null || IsDisposed)
				return;
			try
			{
				if (presenterUpdate.FocusedBeforeUpdate != null
				 && selection.Message != null)
				{
					SetScrollPos(new Point(scrollBarsInfo.scrollPos.X,
						selection.DisplayPosition * drawContext.LineHeight - presenterUpdate.RelativeForcusedScrollPositionBeforeUpdate));
				}
				else
				{
					SetScrollPos(new Point(scrollBarsInfo.scrollPos.X, 0));
				}
			}
			finally
			{
				presenterUpdate = null;
			}
		}

		void IView.InvalidateMessage(DisplayLine line)
		{
			Rectangle r = DrawingUtils.GetMetrics(line, drawContext, false).MessageRect;
			this.Invalidate(r);
		}

		void IView.ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage)
		{
			if (scrollBarsInfo.userIsScrolling)
			{
				return;
			}

			int? newScrollPos = null;

			VisibleMessagesIndexes vl = DrawingUtils.GetVisibleMessages(drawContext, presentationDataAccess, ClientRectangle);

			int extra = showExtraLinesAroundMessage ? 2 : 0;

			if (messageDisplayPosition < vl.fullyVisibleBegin + extra)
				newScrollPos = messageDisplayPosition - extra;
			else if (messageDisplayPosition > vl.fullyVisibleEnd - extra)
				newScrollPos = messageDisplayPosition  - (vl.fullyVisibleEnd - vl.begin) + extra;

			if (newScrollPos.HasValue)
				SetScrollPos(new Point(scrollBarsInfo.scrollPos.X, newScrollPos.Value * drawContext.LineHeight));
		}

		void IView.UpdateScrollSizeToMatchVisibleCount()
		{
			SetScrollSize(new Size(10, GetDisplayMessagesCount() * drawContext.LineHeight), true, false);
		}

		void IView.HScrollToSelectedText(SelectionInfo selection)
		{
			if (selection.First.Message == null)
				return;

			int pixelThatMustBeVisible = (int)(selection.First.LineCharIndex * drawContext.CharSize.Width);
			if (drawContext.ShowTime)
				pixelThatMustBeVisible += drawContext.TimeAreaSize;

			int currentVisibleLeft = scrollBarsInfo.scrollPos.X;
			int currentVisibleRight = scrollBarsInfo.scrollPos.X + drawContext.ViewWidth - SystemInformation.VerticalScrollBarWidth;
			int extraPixelsAroundSelection = 20;
			if (pixelThatMustBeVisible < scrollBarsInfo.scrollPos.X)
			{
				SetScrollPos(new Point(pixelThatMustBeVisible - extraPixelsAroundSelection, scrollBarsInfo.scrollPos.Y));
			}
			if (pixelThatMustBeVisible >= currentVisibleRight)
			{
				SetScrollPos(new Point(scrollBarsInfo.scrollPos.X + (pixelThatMustBeVisible - currentVisibleRight + extraPixelsAroundSelection), 
					scrollBarsInfo.scrollPos.Y));
			}
		}

		void IView.RestartCursorBlinking()
		{
			drawContext.CursorState = true;
		}

		void IView.DisplayEverythingFilteredOutMessage(bool displayOrHide)
		{
			if (everythingFilteredOutMessage == null)
			{
				everythingFilteredOutMessage = new EverythingFilteredOutMessage();
				everythingFilteredOutMessage.Visible = displayOrHide;
				Controls.Add(everythingFilteredOutMessage);
				everythingFilteredOutMessage.Dock = DockStyle.Fill;
				everythingFilteredOutMessage.FiltersLinkLabel.Click += (s, e) => viewEvents.OnShowFiltersLinkClicked();
				everythingFilteredOutMessage.SearchUpLinkLabel.Click += (s, e) => viewEvents.OnSearchNotFilteredMessageLinkClicked(true);
				everythingFilteredOutMessage.SearchDownLinkLabel.Click += (s, e) => viewEvents.OnSearchNotFilteredMessageLinkClicked(false);
			}
			else
			{
				everythingFilteredOutMessage.Visible = displayOrHide;
			}
		}

		void IView.DisplayNothingLoadedMessage(string messageToDisplayOrNull)
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

		void IView.PopupContextMenu(object contextMenuPopupData)
		{
			Point pt;
			if (contextMenuPopupData is Point)
				pt = (Point)contextMenuPopupData;
			else
				pt = new Point();
			DoContextMenu(pt.X, pt.Y);
		}

		void IView.UpdateMillisecondsModeDependentData()
		{
			UpdateTimeAreaSize();
		}

		int IView.DisplayLinesPerPage { get { return Height / drawContext.LineHeight; } }

		object IView.GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection)
		{
			return new Point(0, (selection.DisplayPosition + 1) * drawContext.LineHeight - 1 - drawContext.ScrollPos.Y);
		}

		void IView.AnimateSlaveMessagePosition()
		{
			drawContext.SlaveMessagePositionAnimationStep = 0;
			animationTimer.Enabled = true;
			Invalidate();
		}

		#endregion

		string[] IViewFonts.AvailablePreferredFamilies
		{
			get
			{
				if (availablePreferredFontFamilies == null)
					availablePreferredFontFamilies = GetAvailablePreferredFontFamilies();
				return availablePreferredFontFamilies;
			}
		}

		KeyValuePair<LogFontSize, int>[] IViewFonts.FontSizes
		{
			get
			{
				if (fontSizesMap == null)
					fontSizesMap = MakeFontSizesMap();
				return fontSizesMap;
			}
		}

		#region Overriden event handlers

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (viewEvents == null)
				return;

			if ((Control.ModifierKeys & Keys.Control) != 0)
			{
				viewEvents.OnMouseWheelWithCtrl(e.Delta);
			}
			else
			{
				Rectangle clientRectangle = base.ClientRectangle;
				int p = drawContext.ScrollPos.Y - e.Delta;
				SetScrollPos(new Point(scrollBarsInfo.scrollPos.X, p));
				if (e is HandledMouseEventArgs)
				{
					((HandledMouseEventArgs)e).Handled = true;
				}
			}
			base.OnMouseWheel(e);
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
			bool captureTheMouse = true;

			MessageMouseEventFlag flags = MessageMouseEventFlag.None;
			if (e.Button == MouseButtons.Right)
				flags |= MessageMouseEventFlag.RightMouseButton;
			if (Control.ModifierKeys == Keys.Shift)
				flags |= MessageMouseEventFlag.ShiftIsHeld;
			if (Control.ModifierKeys == Keys.Alt)
				flags |= MessageMouseEventFlag.AltIsHeld;
			if (Control.ModifierKeys == Keys.Control)
				flags |= MessageMouseEventFlag.CtrlIsHeld;
			if (e.Clicks == 2)
				flags |= MessageMouseEventFlag.DblClick;
			else
				flags |= MessageMouseEventFlag.SingleClick;

			DrawingUtils.MouseDownHelper(presentationDataAccess, drawContext, ClientRectangle, viewEvents, e.Location, flags, out captureTheMouse);

			base.OnMouseDown(e);
			
			this.Capture = captureTheMouse;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			DrawingUtils.CursorType newCursor;
			DrawingUtils.MouseMoveHelper(presentationDataAccess, drawContext, ClientRectangle, viewEvents, e.Location,
				e.Button == MouseButtons.Left && this.Capture, out newCursor);

			Cursor newNativeCursor = Cursors.Arrow;
			if (newCursor == DrawingUtils.CursorType.Arrow)
				newNativeCursor = Cursors.Arrow;
			else if (newCursor == DrawingUtils.CursorType.IBeam)
				newNativeCursor = Cursors.IBeam;
			else if (newCursor == DrawingUtils.CursorType.RightToLeftArrow)
				newNativeCursor = rightCursor;
			if (Cursor != newNativeCursor)
				Cursor = newNativeCursor;

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

		protected override void OnKeyDown(KeyEventArgs kevent)
		{
			Keys k = kevent.KeyCode;
			bool ctrl = (kevent.Modifiers & Keys.Control) != 0;
			bool alt = (kevent.Modifiers & Keys.Alt) != 0;
			bool shift = (kevent.Modifiers & Keys.Shift) != 0;

			Key pk;

			if (k == Keys.F5)
				pk = Key.F5;
			else if (k == Keys.Up)
				pk = Key.Up;
			else if (k == Keys.Down)
				pk = Key.Down;
			else if (k == Keys.PageUp)
				pk = Key.PageUp;
			else if (k == Keys.PageDown)
				pk = Key.PageDown;
			else if (k == Keys.Left)
				pk = Key.Left;
			else if (k == Keys.Right)
				pk = Key.Right;
			else if (k == Keys.Apps)
				pk = Key.Apps;
			else if (k == Keys.Enter)
				pk = Key.Enter;
			else if (k == Keys.C && ctrl)
				pk = Key.Copy;
			else if (k == Keys.Insert && ctrl)
				pk = Key.Copy;
			else if (k == Keys.Home)
				pk = Key.Home;
			else if (k == Keys.End)
				pk = Key.End;
			else if (k == Keys.B)
				pk = Key.B;
			else
				pk = Key.None;

			if (viewEvents != null && pk != Key.None)
				viewEvents.OnKeyPressed(pk, ctrl, alt, shift);

			base.OnKeyDown(kevent);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			if (presentationDataAccess != null)
			{
				// Do nothing. All painting is in OnPaint.
			}
			else
			{
				// Draw some background in designer mode
				base.OnPaintBackground(pevent);
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			if (presentationDataAccess == null)
			{
				base.OnPaint(pe);
				return;
			}

			DrawContext dc = drawContext;

			dc.Canvas = new LJD.Graphics(backBufferCanvas.Graphics);

			backBufferCanvas.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, pe.ClipRectangle);
		
			int maxRight;
			DrawingUtils.PaintControl(drawContext, presentationDataAccess, selection, this.Focused, 
				pe.ClipRectangle, out maxRight);

			backBufferCanvas.Render(pe.Graphics);

			UpdateScrollSize(dc, maxRight);

			base.OnPaint(pe);
		}

		protected override void OnResize(EventArgs e)
		{
			drawContext.ViewWidth = this.ClientRectangle.Width;
			EnsureBackbufferIsUpToDate();
			SetScrollPos(scrollBarsInfo.scrollPos);
			Invalidate();
			base.OnResize(e);
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

		#endregion

		#region Implementation

		static List<string> CreateWindowsPreferredFontFamiliesList()
		{
			var preferredFamilies = new List<string> { 
				"consolas",
				"courier new",
				"lucida sans typewriter",
				"lucida console",
				"monaco",
				"menlo",
				"dejavu sans mono",
				"pragmatapro",
				"source code pro",
				"droid sans mono"
			};
			return preferredFamilies;
		}

		static string[] GetAvailablePreferredFontFamilies()
		{
			var availableFamilies = FontFamily.Families.ToLookup(f => f.Name.ToLower());
			return CreateWindowsPreferredFontFamiliesList().Where(f => availableFamilies[f.ToLower()].Any()).ToArray();
		}

		static KeyValuePair<LogFontSize, int>[] MakeFontSizesMap()
		{
			return Enumerable
				.Range((int)LogFontSize.Minimum, LogFontSize.Maximum - LogFontSize.Minimum)
				.Select(i => (LogFontSize)i)
				.Select(lfs => new KeyValuePair<LogFontSize, int>(lfs, ToFontEmSize(lfs)))
				.ToArray();
		}

		FontFamily GetFontFamily(string preferredFamily)
		{
			var preferredFamilies = CreateWindowsPreferredFontFamiliesList();

			if (preferredFamily != null)
			{
				preferredFamily = preferredFamily.ToLower();
				// make it first in the list
				preferredFamilies.Remove(preferredFamily);
				preferredFamilies.Insert(0, preferredFamily);
			}

			var installedFamilies = FontFamily.Families;
			foreach (var candidate in preferredFamilies)
			{
				var installedFamily = installedFamilies.FirstOrDefault(f => string.Compare(f.Name, candidate, true) == 0);
				if (installedFamily != null)
					return installedFamily;
			}

			return FontFamily.GenericMonospace;
		}

		void UpdateTimeAreaSize()
		{
			string testStr = (new MessageTimestamp(new DateTime(2011, 11, 11, 11, 11, 11, 111))).ToUserFrendlyString(drawContext.ShowMilliseconds);
			drawContext.TimeAreaSize = (int)Math.Floor(
				drawContext.CharSize.Width * (float)testStr.Length
			) + 10;
		}

		void DoContextMenu(int x, int y)
		{
			contextMenuStrip1.Show(this, x, y);
		}

		
		Tuple<ToolStripMenuItem, ContextMenuItem>[] MakeMenuItemsMap()
		{
			return new []
			{
				Tuple.Create(this.copyMenuItem, ContextMenuItem.Copy),
				Tuple.Create(this.collapseMenuItem, ContextMenuItem.CollapseExpand),
				Tuple.Create(this.recursiveCollapseMenuItem, ContextMenuItem.RecursiveCollapseExpand),
				Tuple.Create(this.gotoParentFrameMenuItem, ContextMenuItem.GotoParentFrame),
				Tuple.Create(this.gotoEndOfFrameMenuItem, ContextMenuItem.GotoEndOfFrame),
				Tuple.Create(this.showTimeMenuItem, ContextMenuItem.ShowTime),
				Tuple.Create(this.showRawMessagesMenuItem, ContextMenuItem.ShowRawMessages),
				Tuple.Create(this.defaultActionMenuItem, ContextMenuItem.DefaultAction),
				Tuple.Create(this.toggleBmkStripMenuItem, ContextMenuItem.ToggleBmk),
				Tuple.Create(this.gotoNextMessageInTheThreadMenuItem, ContextMenuItem.GotoNextMessageInTheThread),
				Tuple.Create(this.gotoPrevMessageInTheThreadMenuItem, ContextMenuItem.GotoPrevMessageInTheThread),
				Tuple.Create(this.collapseAlllFramesMenuItem, ContextMenuItem.CollapseAllFrames),
				Tuple.Create(this.expandAllFramesMenuItem, ContextMenuItem.ExpandAllFrames)
			};
		}

		void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (viewEvents == null)
				return;
			var clickedItem = menuItemsMap.FirstOrDefault(t => t.Item1 == e.ClickedItem);
			if (clickedItem == null)
				return;
			ContextMenuItem code = clickedItem.Item2;
			bool? itemChecked = null;
			if (code == ContextMenuItem.ShowTime ||
				code == ContextMenuItem.ShowRawMessages)
			{
				itemChecked = (clickedItem.Item1.Checked = !clickedItem.Item1.Checked);
			}
			viewEvents.OnMenuItemClicked(code, itemChecked);
		}

		void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (viewEvents == null)
				return;
			ContextMenuItem visibleItems, checkedItems;
			string defaultItemText = null;
			viewEvents.OnMenuOpening(out visibleItems, out checkedItems, out defaultItemText);
			foreach (var mi in menuItemsMap)
			{
				mi.Item1.Visible = (visibleItems & mi.Item2) != 0;
				mi.Item1.Checked = (checkedItems & mi.Item2) != 0;
			}
			defaultActionMenuItem.Text = defaultItemText;
		}

		void UpdateScrollSize(DrawContext dc, int maxRight)
		{
			maxRight += dc.ScrollPos.X;
			if (maxRight > scrollBarsInfo.scrollSize.Width)
			{
				SetScrollSize(new Size(maxRight, scrollBarsInfo.scrollSize.Height), false, true);
			}
		}

		void EnsureBackbufferIsUpToDate()
		{
			var clientSize = ClientSize;
			if (clientSize.IsEmpty)
				return;
			if (backBufferCanvas == null
			 || clientSize.Width > backBufferCanvasSize.Width
			 || clientSize.Height > backBufferCanvasSize.Height)
			{
				if (backBufferCanvas != null)
					backBufferCanvas.Dispose();
				using (var tmp = this.CreateGraphics())
					backBufferCanvas = bufferedGraphicsContext.Allocate(tmp, new Rectangle(0, 0, clientSize.Width, clientSize.Height));
				backBufferCanvasSize = clientSize;
			}
		}

		void InvalidateMessagesArea()
		{
			Rectangle r = ClientRectangle;
			r.X += drawContext.CollapseBoxesAreaSize;
			r.Width -= drawContext.CollapseBoxesAreaSize;
			Invalidate(r);
		}

		void SetScrollPos(Point pos)
		{
			if (pos.Y > scrollBarsInfo.scrollSize.Height)
				pos.Y = scrollBarsInfo.scrollSize.Height;
			else if (pos.Y < 0)
				pos.Y = 0;

			int maxPosX = Math.Max(0, scrollBarsInfo.scrollSize.Width - ClientSize.Width);

			if (pos.X > maxPosX)
				pos.X = maxPosX;
			else if (pos.X < 0)
				pos.X = 0;

			int xBefore = GetScrollInfo(Native.SB.HORZ).nPos;
			int yBefore = GetScrollInfo(Native.SB.VERT).nPos;

			bool vRedraw = pos.Y != scrollBarsInfo.scrollPos.Y;
			bool hRedraw = pos.X != scrollBarsInfo.scrollPos.X;
			scrollBarsInfo.scrollPos = pos;
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
					r.X += drawContext.CollapseBoxesAreaSize;
					r.Width -= drawContext.CollapseBoxesAreaSize;
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
			drawContext.ScrollPos = new Point(GetScrollInfo(Native.SB.HORZ).nPos, GetScrollInfo(Native.SB.VERT).nPos);
		}

		void SetScrollSize(Size sz, bool vRedraw, bool hRedraw)
		{
			scrollBarsInfo.scrollSize = sz;
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
				v.nMax = scrollBarsInfo.scrollSize.Height;
				v.nPage = ClientRectangle.Height;
				v.nPos = scrollBarsInfo.scrollPos.Y;
				v.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.VERT, ref v, redrawNow && vRedraw);

				Native.SCROLLINFO h = new Native.SCROLLINFO();
				h.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
				h.fMask = Native.SIF.ALL;
				h.nMin = 0;
				h.nMax = scrollBarsInfo.scrollSize.Width;
				h.nPage = ClientRectangle.Width;
				h.nPos = scrollBarsInfo.scrollPos.X;
				h.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.HORZ, ref h, redrawNow && hRedraw);

				if (!redrawNow)
				{
					scrollBarsInfo.vRedraw |= vRedraw;
					scrollBarsInfo.hRedraw |= hRedraw;
					if (!scrollBarsInfo.repaintPosted)
					{
						Native.PostMessage(handle, ScrollBarsInfo.WM_REPAINTSCROLLBARS, IntPtr.Zero, IntPtr.Zero);
						scrollBarsInfo.repaintPosted = true;
					}
				}
			}
		}

		void WMRepaintScrollBars()
		{
			InternalUpdateScrollBars(scrollBarsInfo.vRedraw, scrollBarsInfo.hRedraw, true);
			scrollBarsInfo.repaintPosted = false;
			scrollBarsInfo.hRedraw = false;
			scrollBarsInfo.vRedraw = false;
		}

		void WmHScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, scrollBarsInfo.scrollPos.X, scrollBarsInfo.scrollSize.Width, Native.SB.HORZ);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(ret, scrollBarsInfo.scrollPos.Y));
			}
			if (viewEvents != null)
				viewEvents.OnHScrolled();
		}

		Native.SCROLLINFO GetScrollInfo(Native.SB sb)
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
						scrollBarsInfo.userIsScrolling = true;
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
						scrollBarsInfo.userIsScrolling = false;
						break;
				}

				return num;
			}
		}

		void WmVScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, scrollBarsInfo.scrollPos.Y, scrollBarsInfo.scrollSize.Height, Native.SB.VERT);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(scrollBarsInfo.scrollPos.X, ret));
			}
		}

		int GetDisplayMessagesCount()
		{
			return presentationDataAccess != null ? presentationDataAccess.DisplayMessages.Count : 0;
		}

		#endregion

		#region Data members

		IViewEvents viewEvents;
		IPresentationDataAccess presentationDataAccess;

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
		ScrollBarsInfo scrollBarsInfo;

		class PresenterUpdate
		{
			public IMessage FocusedBeforeUpdate;
			public int RelativeForcusedScrollPositionBeforeUpdate;
		};
		PresenterUpdate presenterUpdate;

		DrawContext drawContext = new DrawContext();
		BufferedGraphics backBufferCanvas;
		Size backBufferCanvasSize;
		Cursor rightCursor;

		BufferedGraphicsContext bufferedGraphicsContext;
		SelectionInfo selection { get { return presentationDataAccess != null ? presentationDataAccess.Selection : new SelectionInfo(); } }
		EverythingFilteredOutMessage everythingFilteredOutMessage;
		EmptyMessagesCollectionMessage emptyMessagesCollectionMessage;
		Tuple<ToolStripMenuItem, ContextMenuItem>[] menuItemsMap;
		string[] availablePreferredFontFamilies;
		KeyValuePair<LogFontSize, int>[] fontSizesMap;

		#endregion
	}
}
