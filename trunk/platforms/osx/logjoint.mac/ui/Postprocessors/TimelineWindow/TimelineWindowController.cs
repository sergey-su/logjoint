
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Drawing;
using LogJoint.Drawing;
using LJD = LogJoint.Drawing;
using CoreText;
using ObjCRuntime;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public partial class TimelineWindowController : 
		AppKit.NSWindowController, 
		IView, 
		Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm
	{
		IViewEvents eventsHandler;
		TagsListViewController tagsListController;
		QuickSearchTextBoxAdapter quickSearchTextBox;
		ToastNotificationsViewAdapter toastNotifications;
		ViewMetrics viewMetrics = new ViewMetrics();
		int sequenceDiagramAreaWidth;
		int lastActivitesCount;

		#region Constructors

		// Called when created from unmanaged code
		public TimelineWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimelineWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public TimelineWindowController () : base ("TimelineWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
			tagsListController = new TagsListViewController ();
			quickSearchTextBox = new QuickSearchTextBoxAdapter ();
			toastNotifications = new ToastNotificationsViewAdapter ();
		}

		#endregion

		//strongly typed window accessor
		public new TimelineWindow Window {
			get {
				return (TimelineWindow)base.Window;
			}
		}

		void PlaceToastNotificationsView(NSView toastNotificationsView, NSView parent)
		{
			parent.AddSubview (toastNotificationsView);
			toastNotificationsView.TranslatesAutoresizingMaskIntoConstraints = false;
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Trailing, 1f, 2f));
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Top, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Top, 1f, 52f));
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Window.owner = this;

			UpdateViewMetrics ();

			activitiesView.PostsFrameChangedNotifications = true;
			captionsView.PostsFrameChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, 
				ns => { UpdateViewMetrics(); UpdateVertScroller(); }, activitiesView);
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, 
				ns => UpdateViewMetrics(), captionsView);

			PlaceToastNotificationsView(toastNotifications.View, activitiesView);

			activitiesView.BackgroundColor = NSColor.White;
			activitiesView.CanBeFirstResponder = true;
			activitiesView.OnPaint = DrawActivitiesView;
			activitiesView.OnScrollWheel = ActivitiesViewScrollWheel;
			activitiesView.OnMagnify = ActivitiesViewMagnify;
			activitiesView.OnMouseDown = 
				e => eventsHandler.OnMouseDown (new HitTestToken(activitiesView, e), GetModifiers(e), e.ClickCount == 2);
			activitiesView.OnMouseUp = 
				e => eventsHandler.OnMouseUp (new HitTestToken(activitiesView, e));
			activitiesView.OnMouseMove = e => {
				SetCursor(e);
				SetToolTip (e);
				eventsHandler.OnMouseMove (new HitTestToken(activitiesView, e), GetModifiers(e));
			};
			activitiesView.OnMouseLeave = e => NSCursor.ArrowCursor.Set ();
			activitiesView.OnMouseDragged = activitiesView.OnMouseMove;

			captionsView.BackgroundColor = NSColor.White;
			captionsView.CanBeFirstResponder = true;
			captionsView.OnPaint = DrawCaptionsView;
			captionsView.OnMouseDown = 
				e => eventsHandler.OnMouseDown (new HitTestToken(captionsView, e), GetModifiers(e), e.ClickCount == 2);
			captionsView.OnMouseUp = 
				e => eventsHandler.OnMouseUp (new HitTestToken(captionsView, e));

			activityDetailsLabel.BackgroundColor = NSColor.White;
			activityDetailsLabel.LinkClicked = (s, e) => eventsHandler.OnActivityTriggerClicked(e.Link.Tag);
			activityLogSourceLabel.BackgroundColor = NSColor.White;
			activityLogSourceLabel.LinkClicked = (s, e) => eventsHandler.OnActivitySourceLinkClicked(e.Link.Tag);

			navigatorView.OnPaint = DrawNavigationPanel;
			navigatorView.OnMouseDown = e => {
				var pt = navigatorView.ConvertPointFromView(e.LocationInWindow, null).ToPoint();
				eventsHandler.OnNavigation ((double)pt.X / (double)viewMetrics.NavigationPanelWidth);
			};

			vertScroller.Action = new Selector ("OnVertScrollChanged");
			vertScroller.Target = this;

			tagsListController.View.MoveToPlaceholder (tagsSelectorPlacefolder);
			quickSearchTextBox.View.MoveToPlaceholder (searchTextBoxPlaceholder);

			Window.InitialFirstResponder = activitiesView;
		}

		partial void OnPing (Foundation.NSObject sender)
		{
		}

		partial void OnActiveNotificationsButtonClicked (NSObject sender)
		{
			eventsHandler.OnActiveNotificationButtonClicked();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Invalidate (ViewAreaFlag flags)
		{
			InvalidateInternal (flags);
		}

		void IView.Refresh (ViewAreaFlag flags)
		{
			InvalidateInternal (flags);
		}

		void IView.UpdateActivitiesScroller (int activitesCount)
		{
			lastActivitesCount = activitesCount;
			UpdateVertScroller ();
		}

		void IView.UpdateCurrentActivityControls (string caption, string descriptionText, IEnumerable<Tuple<object, int, int>> descriptionLinks, string sourceText, Tuple<object, int, int> sourceLink)
		{
			EnsureNibLoadeded ();
			activityNameTextField.StringValue = caption;
			activityDetailsLabel.StringValue = descriptionText;
			if (descriptionLinks != null)
				activityDetailsLabel.Links = descriptionLinks.Select (l => new NSLinkLabel.Link (l.Item2, l.Item3, l.Item1)).ToArray ();
			else
				activityDetailsLabel.Links = null;
			activityLogSourceLabel.StringValue = sourceText;
			if (sourceLink != null)
				activityLogSourceLabel.Links = new [] { new NSLinkLabel.Link (sourceLink.Item2, sourceLink.Item3, sourceLink.Item1) };
			else
				activityLogSourceLabel.Links = null;
		}

		HitTestResult IView.HitTest (object hitTestToken)
		{
			var token = hitTestToken as HitTestToken;
			if (token == null)
				return new HitTestResult();
			var pt = activitiesView.ConvertPointFromView(token.Event.LocationInWindow, null).ToPoint();
			return Metrics.HitTest(pt, viewMetrics, eventsHandler, token.View == captionsView, () => new LJD.Graphics ());
		}

		void IView.EnsureActivityVisible (int activityIndex)
		{
			int y = Metrics.GetActivityY(viewMetrics, activityIndex);
			if (y > 0 && (y + viewMetrics.LineHeight) < viewMetrics.ActivitiesViewHeight)
				return;
			var scrollerPos = viewMetrics.RulersPanelHeight - viewMetrics.ActivitiesViewHeight / 2 
				+ activityIndex * viewMetrics.LineHeight;
			scrollerPos = Math.Max(0, scrollerPos);
			vertScroller.DoubleValue = (double)scrollerPos / VertScrollerValueRange;
			UpdateViewMetrics ();
			InvalidateInternal (ViewAreaFlag.ActivitiesCaptionsView | ViewAreaFlag.ActivitiesBarsView);
		}

		void IView.UpdateSequenceDiagramAreaMetrics ()
		{
			using (var g = new LJD.Graphics ())
				sequenceDiagramAreaWidth = Metrics.GetSequenceDiagramAreaMetrics (g, viewMetrics, eventsHandler);
		}

		void IView.ReceiveInputFocus ()
		{
			activitiesView.Window.MakeFirstResponder (activitiesView);
		}

		LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox 
		{
			get { return quickSearchTextBox; }
		}

		LogJoint.UI.Presenters.TagsList.IView IView.TagsListView
		{
			get { return tagsListController; }
		}

		Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView
		{
			get { return toastNotifications; }
		}

		void IView.SetNotificationsIconVisibility(bool value)
		{
			activeNotificationsButton.Hidden = !value;
		}

		void Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm.Show ()
		{
			Window.MakeKeyAndOrderFront (null);
		}

		internal void OnKeyEvent(KeyCode code)
		{
			eventsHandler.OnKeyDown (code);
		}

		internal void OnCancelOp()
		{
			if (!eventsHandler.OnEscapeCmdKey ())
				Window.Close();
		}

		partial void OnZoomInClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnZoomInButtonClicked();
		}

		partial void OnZoomOutClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnZoomOutButtonClicked();
		}

		partial void OnNextUserActionClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnNextUserEventButtonClicked();
		}

		partial void OnPrevUserActionClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnPrevUserEventButtonClicked();
		}

		partial void OnPrevBookmarkClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnPrevBookmarkButtonClicked();
		}

		partial void OnNextBookmarkClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnNextBookmarkButtonClicked();
		}

		partial void OnCurrentTimeClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnFindCurrentTimeButtonClicked();
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			UpdateViewMetrics ();
			InvalidateInternal (ViewAreaFlag.ActivitiesBarsView | ViewAreaFlag.ActivitiesCaptionsView);
		}

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject sender)
		{
			eventsHandler.OnKeyDown (KeyCode.Find);
		}

		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return true;
		}

		void InvalidateInternal (ViewAreaFlag flags)
		{
			EnsureNibLoadeded ();
			if ((flags & ViewAreaFlag.NavigationPanelView) != 0)
				navigatorView.NeedsDisplay = true;
			if ((flags & ViewAreaFlag.ActivitiesCaptionsView) != 0)
				captionsView.NeedsDisplay = true;
			if ((flags & ViewAreaFlag.ActivitiesBarsView) != 0)
				activitiesView.NeedsDisplay = true;
		}

		void UpdateViewMetrics ()
		{
			viewMetrics.RulersPanelHeight = (int)searchTextBoxPlaceholder.Frame.Bottom;
			viewMetrics.ActivitiesViewWidth = (int)activitiesView.Bounds.Width;
			viewMetrics.ActivitiesViewHeight = (int)activitiesView.Bounds.Height;
			viewMetrics.ActivitesCaptionsViewWidth = (int)captionsView.Bounds.Width;
			viewMetrics.ActivitesCaptionsViewHeight = (int)captionsView.Bounds.Height;
			viewMetrics.NavigationPanelWidth = (int)navigatorView.Bounds.Width;
			viewMetrics.NavigationPanelHeight = (int)navigatorView.Bounds.Height;

			if (viewMetrics.LineHeight == 0)  // very first update
			{
				var sysFont = NSFont.SystemFontOfSize (NSFont.SystemFontSize);
				var normalFont = new LJD.Font (sysFont.FamilyName, (float) NSFont.SystemFontSize);
				var smallFont = new LJD.Font (sysFont.FamilyName, (float) NSFont.SmallSystemFontSize);

				using (var g = new LJD.Graphics ())
					viewMetrics.LineHeight = (int)g.MeasureString ("ABC012", normalFont).Height;

				viewMetrics.ActivitesCaptionsFont = normalFont;
				viewMetrics.ActivitesCaptionsBrush = new LJD.Brush(Color.Black);

				var selectedLineColor = Color.FromArgb(187, 196, 221);
				viewMetrics.SelectedLineBrush = new LJD.Brush(selectedLineColor);

				viewMetrics.RulerLinePen = new LJD.Pen (Color.LightGray, 1);
				viewMetrics.RulerMarkFont = smallFont;
				viewMetrics.RulerMarkBrush = new LJD.Brush (Color.Black);

				viewMetrics.ActivitiesTopBoundPen = new LJD.Pen (Color.Gray, 1);

				viewMetrics.ProcedureBrush = new LJD.Brush(Color.LightBlue);
				viewMetrics.LifetimeBrush = new LJD.Brush(Color.LightGreen);
				viewMetrics.NetworkMessageBrush = new LJD.Brush(Color.LightSalmon);
				viewMetrics.UnknownActivityBrush = new LJD.Brush(Color.LightGray);
				viewMetrics.MilestonePen = new LJD.Pen(Color.FromArgb(180, Color.SteelBlue), 3);
				viewMetrics.ActivityBarBoundsPen = new LJD.Pen (Color.Gray, 0.5f);
				viewMetrics.ActivitiesConnectorPen = new LJD.Pen (Color.DarkGray, 1, new [] { 1f, 1f });

				viewMetrics.UserEventPen = new LJD.Pen(Color.Salmon, 2);
				viewMetrics.EventRectBrush = new LJD.Brush (Color.Salmon);
				viewMetrics.EventRectPen = new LJD.Pen (Color.Gray, 1);
				viewMetrics.EventCaptionBrush = new LJD.Brush (Color.Black);
				viewMetrics.EventCaptionFont = smallFont;
				viewMetrics.ActionLebelHeight = 17;
				viewMetrics.EventCaptionStringFormat = new LJD.StringFormat (StringAlignment.Center, StringAlignment.Far);
				viewMetrics.ActionCaptionFont = smallFont;

				viewMetrics.BookmarkPen = new LJD.Pen(Color.FromArgb(0x5b, 0x87, 0xe0), 1);
				viewMetrics.BookmarkIcon = new LJD.Image(NSImage.ImageNamed("TimelineBookmark.png"));
				viewMetrics.UserIcon = new LJD.Image(NSImage.ImageNamed("UserAction.png"));
				viewMetrics.APIIcon = new LJD.Image(NSImage.ImageNamed("APICall.png"));

				viewMetrics.FocusedMessageLineTop = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlaveVert.png"));
				viewMetrics.FocusedMessagePen = new LJD.Pen (Color.Blue, 1);

				viewMetrics.MeasurerTop = 25;
				viewMetrics.MeasurerPen = new LJD.Pen(Color.DarkGreen, 1f, new[] { 4f, 2f });
				viewMetrics.MeasurerTextFont = smallFont;
				viewMetrics.MeasurerTextBrush = new LJD.Brush (Color.Black);
				viewMetrics.MeasurerTextBoxBrush = new LJD.Brush (Color.White);
				viewMetrics.MeasurerTextBoxPen = new LJD.Pen (Color.DarkGreen, 1f);
				viewMetrics.MeasurerTextFormat = new LJD.StringFormat (StringAlignment.Center, StringAlignment.Center);

				viewMetrics.NavigationPanel_InvisibleBackground = new LJD.Brush(Color.FromArgb(235, 235, 235));
				viewMetrics.NavigationPanel_VisibleBackground = new LJD.Brush (Color.White);
				viewMetrics.VisibleRangePen = new LJD.Pen(Color.Gray, 1f);
				viewMetrics.SystemControlBrush = viewMetrics.NavigationPanel_InvisibleBackground;

				viewMetrics.ActivityBarRectPaddingY = 5;
				viewMetrics.TriggerLinkWidth = 8;
				viewMetrics.DistnanceBetweenRulerMarks = 40;
				viewMetrics.VisibleRangeResizerWidth = 8;
			}

			viewMetrics.VScrollBarValue = (int)(vertScroller.DoubleValue * VertScrollerValueRange);
		}

		double VertScrollerValueRange
		{
			get { return lastActivitesCount * viewMetrics.LineHeight - (viewMetrics.ActivitiesViewHeight - viewMetrics.RulersPanelHeight); }
		}

		void DrawCaptionsView(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) {
				Drawing.DrawCaptionsView (
					g,
					viewMetrics,
					eventsHandler,
					sequenceDiagramAreaWidth,
					(captionText, captionRect, highlightBegin, highlightLen) =>
					{
						var attrString = new NSMutableAttributedString(captionText);
						attrString.AddAttribute(NSStringAttributeKey.ParagraphStyle, new NSMutableParagraphStyle()
						{
							LineBreakMode = NSLineBreakMode.TruncatingTail,
							TighteningFactorForTruncation = 0
						}, new NSRange(0, captionText.Length));
						if (highlightLen > 0 && highlightBegin >= 0 && (highlightBegin + highlightLen <= captionText.Length))
						{
							attrString.AddAttribute(NSStringAttributeKey.BackgroundColor, NSColor.Yellow, 
								new NSRange (highlightBegin, highlightLen));
						}
						attrString.DrawString (LJD.Extensions.ToCGRect (captionRect));
					}
				);
			}
		}

		void DrawActivitiesView(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) 
				Drawing.DrawActivtiesView(g, viewMetrics, eventsHandler);
		}

		void DrawNavigationPanel(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) 
			{
				Drawing.DrawNavigationPanel (g, viewMetrics, eventsHandler);
			}
		}

		void ActivitiesViewScrollWheel(NSEvent evt)
		{
			eventsHandler.OnScrollWheel (-evt.ScrollingDeltaX / (double)viewMetrics.ActivitiesViewWidth);
			if (evt.ScrollingDeltaY != 0)
			{
				if ((evt.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				{
					var pt = activitiesView.ConvertPointFromView(evt.LocationInWindow, null);
					eventsHandler.OnMouseZoom(pt.X / (double)viewMetrics.ActivitiesViewWidth, (int)(-evt.DeltaY * 100));
				}
				else if (vertScroller.Enabled)
				{
					if (evt.HasPreciseScrollingDeltas)
						vertScroller.DoubleValue -= (evt.ScrollingDeltaY / VertScrollerValueRange);
					else
						vertScroller.DoubleValue -= (Math.Sign(evt.ScrollingDeltaY) * 30d / VertScrollerValueRange);
				}
			}
		}
	
		void ActivitiesViewMagnify(NSEvent evt)
		{
			var pt = activitiesView.ConvertPointFromView(evt.LocationInWindow, null);
			eventsHandler.OnGestureZoom(pt.X / (double)viewMetrics.ActivitiesViewWidth, evt.Magnification);
		}

		void EnsureNibLoadeded ()
		{
			Window.GetHashCode ();
		}

		void SetCursor (NSEvent e)
		{
			var pt = activitiesView.ConvertPointFromView (e.LocationInWindow, null).ToPoint ();
			var cursor = Metrics.GetCursor (pt, viewMetrics, eventsHandler, () => new LJD.Graphics ());
			if (cursor == CursorType.Hand)
				NSCursor.PointingHandCursor.Set ();
			else
				NSCursor.ArrowCursor.Set ();
		}

		void UpdateVertScroller ()
		{
			float contentSize = lastActivitesCount * viewMetrics.LineHeight;
			float windowSize = viewMetrics.ActivitiesViewHeight - viewMetrics.RulersPanelHeight;
			var enableScroller = lastActivitesCount > 0 && contentSize > windowSize;
			vertScroller.Enabled = enableScroller;
			vertScroller.Hidden = !enableScroller;
			if (enableScroller) {
				vertScroller.KnobProportion = windowSize / contentSize;
			} else {
				vertScroller.DoubleValue = 0;
			}
			UpdateViewMetrics ();
		}

		static KeyCode GetModifiers(NSEvent e)
		{
			var code = KeyCode.None;
			if ((e.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				code |= KeyCode.Ctrl;
			if ((e.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
				code |= KeyCode.Shift;
			return code;
		}

		void SetToolTip (NSEvent e)
		{
			var toolTip = eventsHandler.OnToolTip (new HitTestToken (activitiesView, e));
			if (toolTip != null)
				activitiesView.ToolTip = toolTip;
			else
				activitiesView.RemoveAllToolTips ();
		}

		class HitTestToken
		{
			public NSView View;
			public NSEvent Event;
			public HitTestToken(NSView view, NSEvent evt)
			{
				this.View = view;
				this.Event = evt;
			}
		};
	}
}