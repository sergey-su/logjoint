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
using System.Threading.Tasks;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSResponder, IView
	{
		internal IViewEvents viewEvents;
		internal IPresentationDataAccess presentationDataAccess;
		internal bool isFocused;
		NSTimer animationTimer;

		[Export("innerView")]
		/// <summary>
		/// Gets or sets the inner view.
		/// </summary>
		/// <value>The inner view.</value>
		public LogViewerControl InnerView { get; set;}


		[Export("view")]
		/// <summary>
		/// Gets or sets the view.
		/// </summary>
		/// <value>The view.</value>
		public NSView View { get; set;}

		[Export("scrollView")]
		/// <summary>
		/// Gets or sets the scroll view.
		/// </summary>
		/// <value>The scroll view.</value>
		public NSScrollView ScrollView { get; set;}

		[Export("vertScroller")]
		/// <summary>
		/// Gets or sets the vert scroller.
		/// </summary>
		/// <value>The vert scroller.</value>
		public NSScroller VertScroller { get; set;}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogJoint.UI.LogViewerControlAdapter"/> class.
		/// </summary>
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
			// without this vert. scrolling with touch gesture often gets stuck
			// if gesture is not absolulety vertical
			ScrollView.HorizontalScrollElasticity = NSScrollElasticity.None;

			ScrollView.PostsFrameChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, async ns =>
			{
				if (viewEvents != null)
					viewEvents.OnDisplayLinesPerPageChanged();
				await Task.Yield(); // w/o this hack inner view is never painted until first resize
				UpdateInnerViewSize();
			}, ScrollView);

			VertScroller.Action = new Selector ("OnVertScrollChanged");
			VertScroller.Target = this;
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
			
			drawContext.Font = new LJD.Font("monaco", 12);

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

			var pos = ScrollView.ContentView.Bounds.Location.ToPoint();

			int currentVisibleLeft = pos.X;
			int VerticalScrollBarWidth = 50; // todo: how to know it on mac?
			int currentVisibleRight = pos.X + (int)ScrollView.Frame.Width - VerticalScrollBarWidth;
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

		void IView.UpdateInnerViewSize()
		{
			UpdateInnerViewSize();
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
			drawContext.SlaveMessagePositionAnimationStep = 0;
			InnerView.NeedsDisplay = true;
			if (animationTimer != null)
				animationTimer.Dispose();
			animationTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(50), () =>
			{
				if (drawContext.SlaveMessagePositionAnimationStep < 8)
				{
					drawContext.SlaveMessagePositionAnimationStep++;
				}
				else
				{
					animationTimer.Dispose();
					animationTimer = null;
					drawContext.SlaveMessagePositionAnimationStep = 0;
				}
				InnerView.NeedsDisplay = true;
			});
		}

		float IView.DisplayLinesPerPage { get { return ScrollView.Frame.Height / (float)drawContext.LineHeight; } }

		void IView.SetVScroll(double? value)
		{
			VertScroller.Enabled = value.HasValue;
			VertScroller.KnobProportion = 0.0001f;
			VertScroller.DoubleValue = value.GetValueOrDefault();
		}

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

		void UpdateInnerViewSize()
		{
			InnerView.Frame = new RectangleF(0, 0, viewWidth, ScrollView.Frame.Height);
		}

		internal void OnPaint(RectangleF dirtyRect)
		{
			UpdateClientSize();

			drawContext.Canvas = new LJD.Graphics();
			drawContext.ScrollPos = new Point(0,
				(int)(presentationDataAccess.GetFirstDisplayMessageScrolledLines() * (double)drawContext.LineHeight));

			int maxRight;
			DrawingUtils.PaintControl(drawContext, presentationDataAccess, selection, isFocused, 
				dirtyRect.ToRectangle(), out maxRight);

			if (maxRight > viewWidth)
			{
				viewWidth = maxRight;
				((IView)this).UpdateInnerViewSize();
			}
		}

		internal void OnScrollWheel(NSEvent e)
		{
			viewEvents.OnIncrementalVScroll(-e.ScrollingDeltaY / drawContext.LineHeight);

			var pos = ScrollView.ContentView.Bounds.Location;
			InnerView.ScrollPoint(new PointF(pos.X - e.ScrollingDeltaX, pos.Y));
		}

		internal void OnMouseDown(NSEvent e)
		{
			MessageMouseEventFlag flags = MessageMouseEventFlag.None;
			if (e.Type == NSEventType.RightMouseDown)
				flags |= MessageMouseEventFlag.RightMouseButton;
			if ((e.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
				flags |= MessageMouseEventFlag.ShiftIsHeld;
			if ((e.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0)
				flags |= MessageMouseEventFlag.AltIsHeld;
			if ((e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				flags |= MessageMouseEventFlag.CtrlIsHeld;
			if (e.ClickCount == 2)
				flags |= MessageMouseEventFlag.DblClick;
			else
				flags |= MessageMouseEventFlag.SingleClick;
			
			bool captureTheMouse;

			DrawingUtils.MouseDownHelper(
				presentationDataAccess,
				drawContext,
				ClientRectangle,
				viewEvents,
				InnerView.ConvertPointFromView (e.LocationInWindow, null).ToPoint(),
				flags,
				out captureTheMouse
			);
		}

		internal void OnMouseMove(NSEvent e, bool dragging)
		{
			DrawingUtils.CursorType cursor;
			DrawingUtils.MouseMoveHelper(
				presentationDataAccess,
				drawContext,
				ClientRectangle,
				viewEvents,
				InnerView.ConvertPointFromView(e.LocationInWindow, null).ToPoint(),
				dragging,
				out cursor
			);
		}

		void InitDrawingContext()
		{
			drawContext.DefaultBackgroundBrush = new LJD.Brush(Color.White);
			drawContext.OutlineMarkupPen = new LJD.Pen(Color.Gray, 1);
			drawContext.InfoMessagesBrush = new LJD.Brush(Color.Black);
			drawContext.CommentsBrush = new LJD.Brush(Color.Gray);
			drawContext.SelectedBkBrush = new LJD.Brush(Color.FromArgb(167, 176, 201));
			//drawContext.SelectedFocuslessBkBrush = new LJD.Brush(Color.Gray);
			drawContext.HighlightBrush = new LJD.Brush(Color.Cyan);
			drawContext.CursorPen = new LJD.Pen(Color.Black, 2);

			int hightlightingAlpha = 170;
			drawContext.InplaceHightlightBackground1 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			drawContext.InplaceHightlightBackground2 =
				new LJD.Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));
			
			drawContext.ErrorIcon = new LJD.Image(NSImage.ImageNamed("ErrorLogSeverity.png"));
			drawContext.WarnIcon = new LJD.Image(NSImage.ImageNamed("WarnLogSeverity.png"));
			drawContext.BookmarkIcon = new LJD.Image(NSImage.ImageNamed("Bookmark.png"));
			drawContext.SmallBookmarkIcon = new LJD.Image(NSImage.ImageNamed("Bookmark.png"));
			drawContext.FocusedMessageIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsg.png"));
			drawContext.FocusedMessageSlaveIcon = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlave.png"));
		}

		void UpdateClientSize()
		{
			drawContext.ViewWidth = viewWidth;
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			viewEvents.OnVScroll(VertScroller.DoubleValue);
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
				default: return 14;
			}
		}


		SelectionInfo selection { get { return presentationDataAccess != null ? presentationDataAccess.Selection : new SelectionInfo(); } }

		Rectangle ClientRectangle
		{
			get { return ScrollView.ContentView.DocumentVisibleRect().ToRectangle(); }
		}

		internal DrawContext DrawContext { get { return drawContext; } }

		DrawContext drawContext = new DrawContext();
		int viewWidth = 2000;
	}
}

