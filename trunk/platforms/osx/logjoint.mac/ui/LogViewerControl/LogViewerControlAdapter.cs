using System;
using System.Collections.Generic;

using System.Drawing;


using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.CoreText;

using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Settings;
using LogJoint.Drawing;
using LJD = LogJoint.Drawing;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using System.Linq;

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSObject, IView
	{
		internal IViewEvents viewEvents;
		internal IPresentationDataAccess presentationDataAccess;

		[Export("innerView")]
		public LogViewerControl InnerView { get; set;}


		[Export("view")]
		public NSScrollView View { get; set;}


		public LogViewerControlAdapter()
		{
			NSBundle.LoadNib ("LogViewerControl", this);
			InnerView.Init(this);
			InitScrollView();
			InitDrawingContext();
			InitCursorTimer();
		}

		void InitCursorTimer()
		{
			NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(500), () =>
				{
					drawContext.CursorState = !drawContext.CursorState;
					if (viewEvents != null)
						viewEvents.OnCursorTimerTick();
				});
		}

		void InitScrollView()
		{
			// this makes scrollers be always visible
			View.ScrollerStyle = NSScrollerStyle.Legacy;
		}

		#region IView implementation

		void IView.SetViewEvents(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetPresentationDataAccess(IPresentationDataAccess presentationDataAccess)
		{
			this.presentationDataAccess = presentationDataAccess;
			this.drawContext.Presenter = presentationDataAccess;
		}

		void IView.UpdateFontDependentData(string fontName, Appearance.LogFontSize fontSize)
		{
			if (drawContext.Font != null)
				drawContext.Font.Dispose();
			
			drawContext.Font = new LJD.Font("Courier", ToFontEmSize(fontSize));

			using (var tmp = new LJD.Graphics()) // todo: consider reusing with windows
			{
				int count = 8 * 1024;
				drawContext.CharSize = tmp.MeasureString(new string('0', count), drawContext.Font);
				drawContext.CharWidthDblPrecision = (double)drawContext.CharSize.Width / (double)count;
				drawContext.CharSize.Width /= (float)count;
				drawContext.LineHeight = (int)Math.Floor(drawContext.CharSize.Height);
			}

			// UpdateTimeAreaSize(); todo
		}

		void IView.SaveViewScrollState(SelectionInfo selection)
		{
			// todo
		}

		void IView.RestoreViewScrollState(SelectionInfo selection)
		{
			// todo
		}

		void IView.HScrollToSelectedText(SelectionInfo selection)
		{
			if (selection.First.Message == null)
				return;

			int pixelThatMustBeVisible = (int)(selection.First.LineCharIndex * drawContext.CharSize.Width);
			if (drawContext.ShowTime)
				pixelThatMustBeVisible += drawContext.TimeAreaSize;

			var pos = View.ContentView.Bounds.Location.ToPoint();

			int currentVisibleLeft = pos.X;
			int VerticalScrollBarWidth = 50; // todo: how to know it on mac?
			int currentVisibleRight = pos.X + (int)View.Frame.Width - VerticalScrollBarWidth;
			int extraPixelsAroundSelection = 20;
			if (pixelThatMustBeVisible < pos.X)
			{
				InnerView.ScrollPoint(new PointF(pixelThatMustBeVisible - extraPixelsAroundSelection, pos.Y));
			}
			if (pixelThatMustBeVisible >= currentVisibleRight)
			{
				InnerView.ScrollPoint(new PointF(pos.X + (pixelThatMustBeVisible - currentVisibleRight + extraPixelsAroundSelection), pos.Y));
			}
		}

		object IView.GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection)
		{
			// todo
			return null;
		}

		void IView.PopupContextMenu(object contextMenuPopupData)
		{
			// todo
		}

		void IView.ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage)
		{
			int? newScrollPos = null;

			// todo: consider resuting with win
			VisibleMessagesIndexes vl = GetVisibleMessages(ClientRectangle);

			int extra = showExtraLinesAroundMessage ? 2 : 0;

			if (messageDisplayPosition < vl.fullyVisibleBegin + extra)
				newScrollPos = messageDisplayPosition - extra;
			else if (messageDisplayPosition > vl.fullyVisibleEnd - extra)
				newScrollPos = messageDisplayPosition  - (vl.fullyVisibleEnd - vl.begin) + extra;

			if (newScrollPos.HasValue)
			{
				var pos = View.ContentView.Bounds.Location;
				InnerView.ScrollPoint(new PointF(pos.X, newScrollPos.Value * drawContext.LineHeight));
			}
		}

		void IView.UpdateScrollSizeToMatchVisibleCount()
		{
			InnerView.Frame = new RectangleF(0, 0, fixedViewWidth, GetDisplayMessagesCount() * drawContext.LineHeight);
		}

		void IView.Invalidate()
		{
			InnerView.NeedsDisplay = true;
		}

		void IView.InvalidateMessage(DisplayLine line)
		{
			Rectangle r = DrawingUtils.GetMetrics(line, drawContext, false).MessageRect;
			InnerView.SetNeedsDisplayInRect(r.ToRectangleF());
		}

		void IView.SetClipboard(string text)
		{
			// todo
		}

		void IView.DisplayEverythingFilteredOutMessage(bool displayOrHide)
		{
			// todo
		}

		void IView.DisplayNothingLoadedMessage(string messageToDisplayOrNull)
		{
			// todo
		}

		void IView.RestartCursorBlinking()
		{
			drawContext.CursorState = true;
		}

		void IView.UpdateMillisecondsModeDependentData()
		{
			// todo
		}

		void IView.AnimateSlaveMessagePosition()
		{
			// todo
		}

		int IView.DisplayLinesPerPage { get { return (int)(InnerView.Frame.Height / drawContext.LineHeight); } }

		#endregion

		#region IViewFonts implementation

		string[] IViewFonts.AvailablePreferredFamilies
		{
			get
			{
				return new string[] { "Courier" };
			}
		}

		KeyValuePair<Appearance.LogFontSize, int>[] IViewFonts.FontSizes
		{
			get
			{
				return new []
				{
					new KeyValuePair<Settings.Appearance.LogFontSize, int>(Appearance.LogFontSize.Normal, 10)
				};
			}
		}

		#endregion



		internal void OnPaint(RectangleF dirtyRect)
		{
			var dc = drawContext;

			UpdateClientSize();

			dc.Canvas = new LJD.Graphics();

			//dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, dirtyRect);

			var drawingVisitor = new DrawingVisitor();
			drawingVisitor.ctx = dc;
			drawingVisitor.InplaceHighlightHandler1 = presentationDataAccess.InplaceHighlightHandler1;
			drawingVisitor.InplaceHighlightHandler2 = presentationDataAccess.InplaceHighlightHandler2;

			var sel = selection;
			bool needToDrawCursor = drawContext.CursorState == true && true /*Focused todo*/ && sel.First.Message != null;

			var messagesToDraw = GetVisibleMessages(dirtyRect.ToRectangle());

			using (var bookmakrsHandler = presentationDataAccess.CreateBookmarksHandler())
			{
				var displayLinesEnum = presentationDataAccess.GetDisplayLines(messagesToDraw.begin, messagesToDraw.end);
				foreach (var il in displayLinesEnum)
				{
					drawingVisitor.DisplayIndex = il.DisplayLineIndex;
					drawingVisitor.TextLineIdx = il.TextLineIndex;
					drawingVisitor.IsBookmarked = bookmakrsHandler.ProcessNextMessageAndCheckIfItIsBookmarked(il.Message);
					DrawingUtils.Metrics m = DrawingUtils.GetMetrics(il, dc, drawingVisitor.IsBookmarked);
					drawingVisitor.m = m;
					if (needToDrawCursor && sel.First.DisplayIndex == il.DisplayLineIndex)
						drawingVisitor.CursorPosition = sel.First;
					else
						drawingVisitor.CursorPosition = null;

					il.Message.Visit(drawingVisitor);

					//maxRight = Math.Max(maxRight, m.OffsetTextRect.Right); todo
				}
			}

		}

		internal void OnMouseDown(NSEvent e)
		{
			var pt = InnerView.ConvertPointFromView (e.LocationInWindow, null).ToPoint();

			bool captureTheMouse = true;
			if (presentationDataAccess != null) // todo: consider moving to drawing utils and reusing with win
			{
				using (var bookmarksHandler = presentationDataAccess.CreateBookmarksHandler())
				{
					foreach (var i in GetVisibleMessagesIterator(ClientRectangle))
					{
						DrawingUtils.Metrics mtx = DrawingUtils.GetMetrics(i, drawContext,
							bookmarksHandler.ProcessNextMessageAndCheckIfItIsBookmarked(i.Message));

						// if user clicked line's outline box (collapse/expand cross)
						if (i.Message.IsStartFrame() && mtx.OulineBox.Contains(pt.X, pt.Y) && i.TextLineIndex == 0)
						if (viewEvents.OnOulineBoxClicked(i.Message, e.ModifierFlags == NSEventModifierMask.ControlKeyMask))
						{
							captureTheMouse = false;
							break;
						}

						// if user clicked line area
						if (mtx.MessageRect.Contains(pt.X, pt.Y))
						{
							var hitTester = new HitTestingVisitor(drawContext, mtx, pt.X, i.TextLineIndex);
							i.Message.Visit(hitTester);

							MessageMouseEventFlag flags = MessageMouseEventFlag.None;
							if (e.Type == NSEventType.RightMouseDown)
								flags |= MessageMouseEventFlag.RightMouseButton;
							if ((e.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
								flags |= MessageMouseEventFlag.ShiftIsHeld;
							if ((e.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0)
								flags |= MessageMouseEventFlag.AltIsHeld;
							if (e.ClickCount == 2)
							{
								flags |= MessageMouseEventFlag.DblClick;
								captureTheMouse = false;
							}
							else
								flags |= MessageMouseEventFlag.SingleClick;
							if (pt.X < FixedMetrics.CollapseBoxesAreaSize)
								flags |= MessageMouseEventFlag.OulineBoxesArea;
							viewEvents.OnMessageMouseEvent(CursorPosition.FromDisplayLine(i, hitTester.LineTextPosition), flags, pt);
							break;
						}
					}
				}
			}
		}
			
		IEnumerable<DisplayLine> GetVisibleMessagesIterator(Rectangle viewRect)
		{
			if (presentationDataAccess == null)
				return Enumerable.Empty<DisplayLine>();
			VisibleMessagesIndexes vl = GetVisibleMessages(viewRect);
			return presentationDataAccess.GetDisplayLines(vl.begin, vl.end);
		}		


		struct VisibleMessagesIndexes
		{
			public int begin;
			public int end;
			public int fullyVisibleBegin;
			public int fullyVisibleEnd;
		};

		VisibleMessagesIndexes GetVisibleMessages(Rectangle viewRect) // todo: move to Drawing utils, share with win
		{
			VisibleMessagesIndexes rv;

			viewRect.Offset(0, drawContext.ScrollPos.Y);

			rv.begin = viewRect.Y / drawContext.LineHeight;
			rv.fullyVisibleBegin = rv.begin;
			if ((viewRect.Y % drawContext.LineHeight) != 0)
				++rv.fullyVisibleBegin;

			rv.end = viewRect.Bottom / drawContext.LineHeight;
			rv.fullyVisibleEnd = rv.end;
			--rv.fullyVisibleEnd;
			if ((viewRect.Bottom % drawContext.LineHeight) != 0)
				++rv.end;

			int visibleCount = GetDisplayMessagesCount();
			rv.begin = Math.Min(visibleCount, rv.begin);
			rv.end = Math.Min(visibleCount, rv.end);
			rv.fullyVisibleEnd = Math.Min(visibleCount, rv.fullyVisibleEnd);

			return rv;
		}

		int GetDisplayMessagesCount()
		{
			return presentationDataAccess != null ? presentationDataAccess.DisplayMessages.Count : 0;
		}


		void InitDrawingContext()
		{
			drawContext.DefaultBackgroundBrush = new LJD.Brush(Color.White);
			drawContext.OutlineMarkupPen = new LJD.Pen(Color.Gray, 1);
			drawContext.InfoMessagesBrush = new LJD.Brush(Color.Black);
			drawContext.CommentsBrush = new LJD.Brush(Color.Gray);
			drawContext.SelectedBkBrush = new LJD.Brush(Color.FromArgb(167, 176, 201));
			//drawContext.SelectedFocuslessBkBrush = new LJD.Brush(Color.Gray);
			drawContext.CursorPen = new LJD.Pen(Color.Black, 2);

			int hightlightingAlpha = 170;
			drawContext.InplaceHightlightBackground1 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			drawContext.InplaceHightlightBackground2 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));
			
			drawContext.ErrorIcon = new LJD.Image(NSImage.ImageNamed("err_small.png"));
			drawContext.WarnIcon = new LJD.Image(NSImage.ImageNamed("warn_small_transp.png"));
			drawContext.BookmarkIcon = new LJD.Image(NSImage.ImageNamed("SmallBookmark.png"));
			drawContext.SmallBookmarkIcon = new LJD.Image(NSImage.ImageNamed("SmallBookmark.png"));
			drawContext.FocusedMessageIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsg.png"));
			drawContext.FocusedMessageSlaveIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlave.png"));
		}

		void UpdateClientSize()
		{
			drawContext.ViewWidth = fixedViewWidth;
		}

		private static int ToFontEmSize(LogFontSize fontSize) // todo: review sizes
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
				default: return 11;
			}
		}

		SelectionInfo selection { get { return presentationDataAccess != null ? presentationDataAccess.Selection : new SelectionInfo(); } }

		Rectangle ClientRectangle
		{
			get { return View.ContentView.DocumentVisibleRect().ToRectangle(); }
		}

		DrawContext drawContext = new DrawContext();
		const int fixedViewWidth = 3000; // todo: stop using fixed width
	}
}

