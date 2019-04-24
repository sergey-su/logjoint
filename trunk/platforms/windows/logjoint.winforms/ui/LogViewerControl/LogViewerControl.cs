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

			drawContext = new DrawContext(fontData =>
			{
				var font = new LJD.Font(GetFontFamily(fontData.Name).Name, ToFontEmSize(fontData.Size));
				using (var nativeGraphics = CreateGraphics())
				using (var tmp = new LJD.Graphics(nativeGraphics))
				{
					int count = 8 * 1024;
					var charSize = tmp.MeasureString(new string('0', count), font);
					var charWidth = (double)charSize.Width / (double)count;
					charSize.Width /= (float)count;
					return (font, charSize, charWidth);
				}
			});

			drawContext.CollapseBoxesAreaSize = UIUtils.Dpi.Scale(drawContext.CollapseBoxesAreaSize);
			drawContext.DpiScale = UIUtils.Dpi.Scale(1f);

			var prototypeStringFormat = (StringFormat)StringFormat.GenericDefault.Clone();
			prototypeStringFormat.SetTabStops(0, new float[] { 20 });
			prototypeStringFormat.FormatFlags |= 
				StringFormatFlags.MeasureTrailingSpaces | 
				StringFormatFlags.NoFontFallback; // this is to treat \0002 and \0003 as regular characters
			drawContext.TextFormat = new LJD.StringFormat(prototypeStringFormat);

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

			int hightlightingAlpha = 170;
			drawContext.SearchResultHighlightingBackground =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			drawContext.SelectionHighlightingBackground =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));

			rightCursor = new System.Windows.Forms.Cursor(
				System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("LogJoint.ui.LogViewerControl.cursor_r.cur"));

			drawContext.ViewWidth = this.ClientRectangle.Width;

			scrollBarsInfo.scrollBarsSize = new Size(SystemInformation.VerticalScrollBarWidth, SystemInformation.HorizontalScrollBarHeight);

			EnsureBackbufferIsUpToDate();
			
			menuItemsMap = MakeMenuItemsMap();
		}

		#region IView members

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

		static void FindRepaintRegions(IReadOnlyList<ViewLine> list1, IReadOnlyList<ViewLine> list2)
		{
			var LookingForFirstMatch = 0;
			var LookingForDiff = 1;
			var LookingForSecondMatch = 2;

			var state = LookingForFirstMatch;
			int idx1 = 0;
			int idx2 = 0;
			while (idx1 < list1.Count && idx2 < list2.Count)
			{
				var cmp = ViewLine.Compare(list1[idx1], list2[idx2]);
				if (state == LookingForFirstMatch)
				{
					if (cmp == (0, false))
					{
						state = LookingForDiff;
					}
				}
				else if (state == LookingForDiff)
				{
					if (cmp != (0, false))
					{
						state = LookingForSecondMatch;
					}
				}
				else if (state == LookingForSecondMatch)
				{
				}

				if (cmp.relativeOrder >= 0)
					++idx1;
				if (cmp.relativeOrder <= 0)
					++idx2;
			}
			// 
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;
			this.drawContext.Presenter = viewModel;

			var viewUpdater = Updaters.Create(
				() => viewModel.ViewLines,
				() => viewModel.FocusedMessageMark,
				(_1, _2) =>
				{
					Invalidate();
				}
			);
			var vScrollerUpdater = Updaters.Create(
				() => viewModel.VerticalScrollerPosition,
				value =>
				{
					scrollBarsInfo.scrollSize.Height = value != null ? ScrollBarsInfo.virtualVScrollSize : 0;
					SetScrollPos(posY: (int)(value.GetValueOrDefault() * (double)(ScrollBarsInfo.virtualVScrollSize - ClientRectangle.Height + 1)));
				}
			);
			var emptyViewMessageUpdater = Updaters.Create(
				() => viewModel.EmptyViewMessage,
				value =>
				{
					if (emptyMessagesCollectionMessage == null)
						Controls.Add(emptyMessagesCollectionMessage = new EmptyMessagesCollectionMessage { Dock = DockStyle.Fill });
					emptyMessagesCollectionMessage.Visible = value != null;
					emptyMessagesCollectionMessage.SetMessage(value ?? "");
				}
			);
			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				viewUpdater();
				vScrollerUpdater();
				emptyViewMessageUpdater();
			});
		}

		bool IView.HasInputFocus
		{
			get { return this.Focused; }
		}

		void IView.ReceiveInputFocus()
		{
			if (CanFocus)
				Focus();
		}

		void IView.HScrollToSelectedText(int charIndex)
		{
			int pixelThatMustBeVisible = (int)(charIndex * drawContext.CharSize.Width) + drawContext.TimeAreaSize;

			int currentVisibleLeft = scrollBarsInfo.scrollPos.X;
			int currentVisibleRight = scrollBarsInfo.scrollPos.X + drawContext.ViewWidth - scrollBarsInfo.scrollBarsSize.Width;
			int extraPixelsAroundSelection = 30;
			if (pixelThatMustBeVisible < scrollBarsInfo.scrollPos.X)
			{
				SetScrollPos(posX: pixelThatMustBeVisible - extraPixelsAroundSelection);
			}
			if (pixelThatMustBeVisible >= currentVisibleRight)
			{
				SetScrollPos(posX: scrollBarsInfo.scrollPos.X + (pixelThatMustBeVisible - currentVisibleRight + extraPixelsAroundSelection));
			}
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

		float IView.DisplayLinesPerPage => Math.Max(0, (float)(ClientSize.Height) / (float)drawContext.LineHeight);

		object IView.GetContextMenuPopupData(int? viewLineIndex)
		{
			if (viewLineIndex.HasValue)
				return new Point(0, DrawingUtils.GetMessageRect(viewModel.ViewLines[viewLineIndex.Value], drawContext).Bottom);
			return new Point();
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

		#region Overridden event handlers

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (viewModel == null)
				return;

			if ((Control.ModifierKeys & Keys.Control) != 0)
			{
				viewModel.OnMouseWheelWithCtrl(e.Delta);
			}
			else
			{
				viewModel.OnIncrementalVScroll(-(float)e.Delta / (float)drawContext.LineHeight);
				if (e is HandledMouseEventArgs)
				{
					((HandledMouseEventArgs)e).Handled = true;
				}
			}
			base.OnMouseWheel(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			viewModel?.ChangeNotification?.Post();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			viewModel?.ChangeNotification?.Post();
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

			DrawingUtils.MouseDownHelper(viewModel, drawContext, ClientRectangle, e.Location, flags, out captureTheMouse);

			base.OnMouseDown(e);
			
			this.Capture = captureTheMouse;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			DrawingUtils.CursorType newCursor;
			DrawingUtils.MouseMoveHelper(viewModel, drawContext, ClientRectangle, e.Location,
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
				pk = Key.Refresh;
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
				pk = Key.ContextMenu;
			else if (k == Keys.Enter)
				pk = Key.Enter;
			else if (k == Keys.C && ctrl)
				pk = Key.Copy;
			else if (k == Keys.Insert && ctrl)
				pk = Key.Copy;
			else if (k == Keys.Home && ctrl)
				pk = Key.BeginOfDocument;
			else if (k == Keys.Home)
				pk = Key.BeginOfLine;
			else if (k == Keys.End && ctrl)
				pk = Key.EndOfDocument;
			else if (k == Keys.End)
				pk = Key.EndOfLine;
			else if (k == Keys.B)
				pk = Key.BookmarkShortcut;
			else
				pk = Key.None;

			if (viewModel != null && pk != Key.None)
			{
				if (ctrl)
					pk |= Key.JumpOverWordsModifier;
				if (alt)
					pk |= Key.AlternativeModeModifier;
				if (shift)
					pk |= Key.ModifySelectionModifier;
				viewModel.OnKeyPressed(pk);
			}

			base.OnKeyDown(kevent);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			if (viewModel != null)
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
			if (viewModel == null)
			{
				base.OnPaint(pe);
				return;
			}

			try
			{
				DrawContext dc = drawContext;

				dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, pe.ClipRectangle);

				UpdateDrawContextScrollPos();

				int maxRight;
				DrawingUtils.PaintControl(drawContext, viewModel, this.Focused, pe.ClipRectangle, out maxRight,
					drawViewLinesAggregaredText: false);

				backBufferCanvas.Render(pe.Graphics);

				UpdateScrollSize(dc, maxRight);
			}
			catch (Exception e)
			{
				if (viewModel != null)
					viewModel.OnDrawingError(e);
				throw;
			}

			base.OnPaint(pe);
		}

		protected override void OnResize(EventArgs e)
		{
			drawContext.ViewWidth = this.ClientRectangle.Width;
			EnsureBackbufferIsUpToDate();
			SetScrollPos();
			Invalidate();
			viewModel?.ChangeNotification?.Post();
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
				"droid sans mono",
				"hack"
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
			if (viewModel == null)
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
			viewModel.OnMenuItemClicked(code, itemChecked);
		}

		void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (viewModel == null)
				return;
			var menuData = viewModel.OnMenuOpening();
			foreach (var mi in menuItemsMap)
			{
				mi.Item1.Visible = (menuData.VisibleItems & mi.Item2) != 0;
				mi.Item1.Checked = (menuData.CheckedItems & mi.Item2) != 0;
			}
			defaultActionMenuItem.Text = menuData.DefaultItemText;

			if (lastExtendedItems != null)
			{
				lastExtendedItems.ForEach(i => i.Dispose());
				lastExtendedItems = null;
			}
			if (menuData.ExtendededItems != null)
			{
				lastExtendedItems = new List<ToolStripItem>();
				menuData.ExtendededItems.ForEach(i =>
				{
					if (lastExtendedItems.Count == 0)
					{
						var separator = new ToolStripSeparator();
						lastExtendedItems.Add(separator);
						contextMenuStrip1.Items.Add(separator);
					}
					var newitem = new ToolStripMenuItem(i.Text);
					if (i.Click != null)
						newitem.Click += (s, evt) => i.Click();
					lastExtendedItems.Add(newitem);
					contextMenuStrip1.Items.Add(newitem);
				});
			}
		}

		void UpdateScrollSize(DrawContext dc, int maxRight)
		{
			maxRight += dc.ScrollPos.X;
			if (maxRight > scrollBarsInfo.scrollSize.Width // if view grows
			|| (maxRight == 0 && scrollBarsInfo.scrollSize.Width != 0 && viewModel != null && viewModel.ViewLines.Length == 0) // or no lines are displayed
			)
			{
				// update the horz scroll bar
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
				drawContext.Canvas = new LJD.Graphics(backBufferCanvas.Graphics);
				backBufferCanvas.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			}
		}

		void SetScrollPos(int? posX = null, int? posY = null)
		{
			var pos = scrollBarsInfo.scrollPos;
			if (posX != null)
				pos.X = posX.Value;
			if (posY != null)
				pos.Y = posY.Value;
			bool scrollDC = posX != null;

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
			else if (scrollDC)
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
			UpdateDrawContextScrollPos();
		}

		private void UpdateDrawContextScrollPos()
		{
			if (viewModel != null)
			{
				drawContext.ScrollPos = new Point(
					GetScrollInfo(Native.SB.HORZ).nPos,
					(int)(viewModel.FirstDisplayMessageScrolledLines * (double)drawContext.LineHeight)
				);
			}
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
			int ret = DoWmScroll(ref m, scrollBarsInfo.scrollPos.X, scrollBarsInfo.scrollSize.Width, Native.SB.HORZ).absoluteScrollPos;
			if (ret >= 0)
			{
				this.SetScrollPos(posX: ret);
			}
			if (viewModel != null)
			{
				viewModel.OnHScroll();
			}
		}

		Native.SCROLLINFO GetScrollInfo(Native.SB sb)
		{
			Native.SCROLLINFO si = new Native.SCROLLINFO();
			si.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
			si.fMask = Native.SIF.ALL;
			Native.GetScrollInfo(new HandleRef(this, base.Handle), sb, ref si);
			return si;
		}

		struct WmScrollResult
		{
			public bool isScrollEvent;
			public bool isRealtimeScroll;
			public int absoluteScrollPos;
			public int delta;
		};

		WmScrollResult DoWmScroll(ref System.Windows.Forms.Message m,
			int num, int maximum, Native.SB bar)
		{
			var ret = new WmScrollResult();
			if (m.LParam != IntPtr.Zero)
			{
				ret.absoluteScrollPos = num;
				base.WndProc(ref m);
				return ret;
			}
			else
			{
				int smallChange = 50;
				int largeChange = 200;
				ret.isScrollEvent = true;

				Native.SB sbEvt = (Native.SB)Native.LOWORD(m.WParam);
				switch (sbEvt)
				{
					case Native.SB.LINEUP:
						num += (ret.delta = -smallChange);
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.LINEDOWN:
						num += (ret.delta = smallChange);
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.PAGEUP:
						num += (ret.delta = -largeChange);
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.PAGEDOWN:
						num += (ret.delta = largeChange);
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.THUMBTRACK:
						num = this.GetScrollInfo(bar).nTrackPos;
						ret.isRealtimeScroll = true;
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
						ret.isScrollEvent = false;
						break;
				}

				ret.absoluteScrollPos = num;

				return ret;
			}
		}

		void WmVScroll(ref System.Windows.Forms.Message m)
		{
			var scrollRlst = DoWmScroll(ref m, scrollBarsInfo.scrollPos.Y, scrollBarsInfo.scrollSize.Height, Native.SB.VERT);
			if (scrollRlst.isScrollEvent)
			{
				if (scrollRlst.delta != 0)
				{
					viewModel.OnIncrementalVScroll((float)scrollRlst.delta / (float)drawContext.LineHeight);
				}
				else if (scrollRlst.absoluteScrollPos >= 0)
				{
					var pos = (double)scrollRlst.absoluteScrollPos / (double)(ScrollBarsInfo.virtualVScrollSize - ClientRectangle.Height + 1);
					viewModel.OnVScroll(
						Math.Min(pos, 1d), 
						scrollRlst.isRealtimeScroll
					);
				}
			}
		}

		#endregion

		#region Data members

		IViewModel viewModel;

		struct ScrollBarsInfo
		{
			public const int WM_REPAINTSCROLLBARS = Native.WM_USER + 98;
			public const int virtualVScrollSize = 100000;
			public Size scrollBarsSize;
			public Point scrollPos;
			public Size scrollSize;
			public bool vRedraw;
			public bool hRedraw;
			public bool repaintPosted;
			public bool userIsScrolling;
		};
		ScrollBarsInfo scrollBarsInfo;

		DrawContext drawContext;
		BufferedGraphics backBufferCanvas;
		Size backBufferCanvasSize;
		Cursor rightCursor;

		BufferedGraphicsContext bufferedGraphicsContext;
		EmptyMessagesCollectionMessage emptyMessagesCollectionMessage;
		Tuple<ToolStripMenuItem, ContextMenuItem>[] menuItemsMap;
		List<ToolStripItem> lastExtendedItems;
		string[] availablePreferredFontFamilies;
		KeyValuePair<LogFontSize, int>[] fontSizesMap;

		#endregion
	}
}
