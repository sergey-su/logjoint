
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
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
		IViewModel model;
		IChangeNotification changeNotification;
		readonly TagsListViewController tagsListController;
		readonly QuickSearchTextBoxAdapter quickSearchTextBox;
		readonly ToastNotificationsViewAdapter toastNotifications;
		CaptionsMarginMetrics captionsMarginMetrics;
		Lazy<GraphicsResources> res;
		Lazy<ControlDrawing> drawing;

		public TimelineWindowController () :
			base ("TimelineWindow")
		{
			tagsListController = new TagsListViewController ();
			quickSearchTextBox = new QuickSearchTextBoxAdapter ();
			toastNotifications = new ToastNotificationsViewAdapter ();
		}

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

			var sysFont = NSFont.SystemFontOfSize (NSFont.SystemFontSize);
			res = new Lazy<GraphicsResources>(() => new GraphicsResources (
				model,
				sysFont.FamilyName,
				(float)NSFont.SystemFontSize,
				(float)NSFont.SmallSystemFontSize,
				(float)NSFont.SmallSystemFontSize,
				new LJD.Image (NSImage.ImageNamed ("UserAction.png")),
				new LJD.Image (NSImage.ImageNamed ("APICall.png")),
				new LJD.Image (NSImage.ImageNamed ("TimelineBookmark.png")),
				new LJD.Image (NSImage.ImageNamed ("FocusedMsgSlaveVert.png")),
				1f,
				new LJD.Brush (Color.FromArgb (235, 235, 235))
			));
			drawing = new Lazy<ControlDrawing>(() => new ControlDrawing (res.Value));

			activitiesView.PostsFrameChangedNotifications = true;
			captionsView.PostsFrameChangedNotifications = true;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, 
				ns => { UpdateVertScroller(); }, activitiesView);

			PlaceToastNotificationsView(toastNotifications.View, activitiesView);

			activitiesView.BackgroundColor = NSColor.TextBackground;
			activitiesView.CanBeFirstResponder = true;
			activitiesView.OnPaint = DrawActivitiesView;
			activitiesView.OnScrollWheel = ActivitiesViewScrollWheel;
			activitiesView.OnMagnify = ActivitiesViewMagnify;
			activitiesView.OnMouseDown = 
				e => model.OnMouseDown (new HitTestToken(activitiesView, e), GetModifiers(e), e.ClickCount == 2);
			activitiesView.OnMouseUp = 
				e => model.OnMouseUp (new HitTestToken(activitiesView, e));
			activitiesView.OnMouseMove = e => {
				SetActivitiesCursor(e);
				SetActivitiesToolTip(e);
				model.OnMouseMove (new HitTestToken(activitiesView, e), GetModifiers(e));
			};
			activitiesView.OnMouseLeave = e => NSCursor.ArrowCursor.Set ();
			activitiesView.OnMouseDragged = activitiesView.OnMouseMove;

			captionsView.BackgroundColor = NSColor.TextBackground;
			captionsView.CanBeFirstResponder = true;
			captionsView.OnPaint = DrawCaptionsView;
			captionsView.OnMouseDown = 
				e => model.OnMouseDown (new HitTestToken(captionsView, e), GetModifiers(e), e.ClickCount == 2);
			captionsView.OnMouseUp = 
				e => model.OnMouseUp (new HitTestToken(captionsView, e));

			activityDetailsLabel.BackgroundColor = NSColor.TextBackground;
			activityDetailsLabel.LinkClicked = (s, e) => model.OnActivityTriggerClicked(e.Link.Tag);
			activityLogSourceLabel.BackgroundColor = NSColor.TextBackground;
			activityLogSourceLabel.LinkClicked = (s, e) => model.OnActivitySourceLinkClicked(e.Link.Tag);

			navigatorView.OnPaint = DrawNavigationPanel;
			navigatorView.OnMouseDown =
				e => model.OnMouseDown (new HitTestToken (navigatorView, e), GetModifiers (e), e.ClickCount == 2);
			navigatorView.OnMouseUp =
				e => model.OnMouseUp (new HitTestToken (navigatorView, e));
			navigatorView.OnMouseMove = (e) => {
				SetNavigationCursor (e);
				model.OnMouseMove (new HitTestToken (navigatorView, e), GetModifiers (e));
			};
			navigatorView.OnMouseDragged = navigatorView.OnMouseMove;
			navigatorView.OnMouseLeave = e => NSCursor.ArrowCursor.Set();

			vertScroller.Action = new Selector ("OnVertScrollChanged");
			vertScroller.Target = this;

			tagsListController.View.MoveToPlaceholder (tagsSelectorPlacefolder);
			quickSearchTextBox.View.MoveToPlaceholder (searchTextBoxPlaceholder);

			Window.InitialFirstResponder = activitiesView;

			var updateNotificationsButton = Updaters.Create (() => model.NotificationsIconVisibile, v => activeNotificationsButton.Hidden = !v);
			var updateNoContentMessage = Updaters.Create (() => model.NoContentMessageVisibile, SetNoContentMessageVisibility);
			var updateVertScroller = Updaters.Create (() => model.ActivitiesCount, _ => UpdateVertScroller ());
			var updateCurrentActivityInfo = Updaters.Create (() => model.CurrentActivity, UpdateCurrentActivityControls);
			changeNotification.CreateSubscription (() => {
				updateNotificationsButton ();
				updateNoContentMessage ();
				updateVertScroller ();
				updateCurrentActivityInfo ();
			});

			Window.WillClose += (s, e) => model.OnWindowHidden ();
		}

		partial void OnPing (Foundation.NSObject sender)
		{
		}

		partial void OnActiveNotificationsButtonClicked (NSObject sender)
		{
			model.OnActiveNotificationButtonClicked();
		}

		void IView.SetViewModel (IViewModel viewModel)
		{
			this.model = viewModel;
			this.changeNotification = viewModel.ChangeNotification;
		}

		void IView.Invalidate (ViewAreaFlag flags)
		{
			InvalidateInternal (flags);
		}

		void IView.Refresh (ViewAreaFlag flags)
		{
			InvalidateInternal (flags);
		}

		void UpdateCurrentActivityControls (CurrentActivityDrawInfo data)
		{
			EnsureNibLoadeded ();
			activityNameTextField.StringValue = data.Caption;
			activityDetailsLabel.StringValue = data.DescriptionText;
			if (data.DescriptionLinks != null)
				activityDetailsLabel.Links = data.DescriptionLinks.Select (l => new NSLinkLabel.Link (l.Item2, l.Item3, l.Item1)).ToArray ();
			else
				activityDetailsLabel.Links = null;
			activityLogSourceLabel.StringValue = data.SourceText;
			if (data.SourceLink != null)
				activityLogSourceLabel.Links = new [] { new NSLinkLabel.Link (data.SourceLink.Item2, data.SourceLink.Item3, data.SourceLink.Item1) };
			else
				activityLogSourceLabel.Links = null;
		}

		HitTestResult IView.HitTest (object hitTestToken)
		{
			var token = hitTestToken as HitTestToken;
			if (token == null)
				return new HitTestResult();
			var pt = token.View.ConvertPointFromView(token.Event.LocationInWindow, null).ToPoint();
			return MakeViewMetrics().HitTest(pt, model, 
				token.View == captionsView ? HitTestResult.AreaCode.CaptionsPanel :
				token.View == navigatorView ? HitTestResult.AreaCode.NavigationPanel :
				HitTestResult.AreaCode.ActivitiesPanel,
				() => new LJD.Graphics ());
		}

		void IView.EnsureActivityVisible (int activityIndex)
		{
			var viewMetrics = MakeViewMetrics ();
			int y = viewMetrics.GetActivityY(activityIndex);
			if (y > 0 && (y + viewMetrics.LineHeight) < viewMetrics.ActivitiesViewHeight)
				return;
			var scrollerPos = viewMetrics.RulersPanelHeight - viewMetrics.ActivitiesViewHeight / 2 
				+ activityIndex * viewMetrics.LineHeight;
			scrollerPos = Math.Max(0, scrollerPos);
			vertScroller.DoubleValue = (double)scrollerPos / GetVertScrollerValueRange(viewMetrics);
			InvalidateInternal (ViewAreaFlag.ActivitiesCaptionsView | ViewAreaFlag.ActivitiesBarsView);
		}

		void IView.UpdateSequenceDiagramAreaMetrics ()
		{
			using (var g = new LJD.Graphics ()) {
				captionsMarginMetrics = MakeViewMetrics().ComputeCaptionsMarginMetrics (g, model);
			}
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

		void SetNoContentMessageVisibility (bool value)
		{
			if (string.IsNullOrEmpty (noContentLink.StringValue)) {
				noContentLink.StringValue = " Nothing visible.\n Search <<left. Search right>> ";
				noContentLink.Links = new [] {
					new NSLinkLabel.Link (26, 6, "l"),
					new NSLinkLabel.Link (41, 7, "r")
				};
				noContentLink.TextColor = NSColor.Black;
				noContentLink.BackgroundColor = NSColor.FromCalibratedRgba (0.941f, 0.678f, 0.305f, 1f);
				noContentLink.LinkClicked = (sender, e) => {
					model.OnNoContentLinkClicked (searchLeft: e.Link.Tag as string == "l");
					e.SuppressDefault = true;
				};
			}
			noContentLink.Hidden = !value;
		}

		void Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm.Show ()
		{
			model.OnWindowShown ();
			Window.MakeKeyAndOrderFront (null);
		}

		internal void OnKeyEvent(KeyCode code)
		{
			model.OnKeyDown (code);
		}

		internal void OnCancelOp()
		{
			if (!model.OnEscapeCmdKey ())
				Window.Close();
		}

		partial void OnZoomInClicked (Foundation.NSObject sender)
		{
			model.OnZoomInButtonClicked();
		}

		partial void OnZoomOutClicked (Foundation.NSObject sender)
		{
			model.OnZoomOutButtonClicked();
		}

		partial void OnNextUserActionClicked (Foundation.NSObject sender)
		{
			model.OnNextUserEventButtonClicked();
		}

		partial void OnPrevUserActionClicked (Foundation.NSObject sender)
		{
			model.OnPrevUserEventButtonClicked();
		}

		partial void OnPrevBookmarkClicked (Foundation.NSObject sender)
		{
			model.OnPrevBookmarkButtonClicked();
		}

		partial void OnNextBookmarkClicked (Foundation.NSObject sender)
		{
			model.OnNextBookmarkButtonClicked();
		}

		partial void OnCurrentTimeClicked (Foundation.NSObject sender)
		{
			model.OnFindCurrentTimeButtonClicked();
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			InvalidateInternal (ViewAreaFlag.ActivitiesBarsView | ViewAreaFlag.ActivitiesCaptionsView);
		}

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject sender)
		{
			model.OnKeyDown (KeyCode.Find);
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

		ViewMetrics MakeViewMetrics ()
		{
			var viewMetrics = new ViewMetrics (res.Value);

			viewMetrics.RulersPanelHeight = (int)searchTextBoxPlaceholder.Frame.Bottom;
			viewMetrics.ActivitiesViewWidth = (int)activitiesView.Bounds.Width;
			viewMetrics.ActivitiesViewHeight = (int)activitiesView.Bounds.Height;
			viewMetrics.ActivitesCaptionsViewWidth = (int)captionsView.Bounds.Width;
			viewMetrics.ActivitesCaptionsViewHeight = (int)captionsView.Bounds.Height;
			viewMetrics.NavigationPanelWidth = (int)navigatorView.Bounds.Width;
			viewMetrics.NavigationPanelHeight = (int)navigatorView.Bounds.Height;

			viewMetrics.LineHeight = (int)(NSFont.SystemFontSize * 1.5f);

			viewMetrics.ActionLebelHeight = 17;
			viewMetrics.MeasurerTop = 25;

			viewMetrics.ActivityBarRectPaddingY = 5;
			viewMetrics.TriggerLinkWidth = 8;
			viewMetrics.DistanceBetweenRulerMarks = 70;
			viewMetrics.VisibleRangeResizerWidth = 8;

			viewMetrics.VScrollBarValue = (int)(vertScroller.DoubleValue * GetVertScrollerValueRange(viewMetrics));

			viewMetrics.SequenceDiagramAreaWidth = captionsMarginMetrics.SequenceDiagramAreaWidth;
			viewMetrics.FoldingAreaWidth = captionsMarginMetrics.FoldingAreaWidth;

			return viewMetrics;
		}

		double GetVertScrollerValueRange(ViewMetrics viewMetrics)
		{
			return Math.Max(0, model.ActivitiesCount * viewMetrics.LineHeight - (viewMetrics.ActivitiesViewHeight - viewMetrics.RulersPanelHeight));
		}

		void DrawCaptionsView(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) {
				drawing.Value.DrawCaptionsView (
					g,
					MakeViewMetrics(),
					model,
					(captionText, captionRect, highlightBegin, highlightLen, isError) =>
					{
						var attrString = new NSMutableAttributedString(captionText);
						attrString.AddAttribute(NSStringAttributeKey.ParagraphStyle, new NSMutableParagraphStyle()
						{
							LineBreakMode = NSLineBreakMode.TruncatingTail,
							TighteningFactorForTruncation = 0
						}, new NSRange(0, captionText.Length));
						if (isError)
						{
							attrString.AddAttribute (NSStringAttributeKey.ForegroundColor, NSColor.Red,
								new NSRange (0, captionText.Length));
						}
						else
						{
							attrString.AddAttribute (NSStringAttributeKey.ForegroundColor, NSColor.Text,
								new NSRange (0, captionText.Length));
						}
						if (highlightLen > 0 && highlightBegin >= 0 && (highlightBegin + highlightLen <= captionText.Length))
						{
							var r = new NSRange (highlightBegin, highlightLen);
							attrString.AddAttribute(NSStringAttributeKey.BackgroundColor, NSColor.Yellow, r);
							attrString.AddAttribute (NSStringAttributeKey.ForegroundColor, NSColor.Black, r);
						}
						attrString.DrawString (LJD.Extensions.ToCGRect (captionRect));
					}
				);
			}
		}

		void DrawActivitiesView(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) 
				drawing.Value.DrawActivtiesView(g, MakeViewMetrics(), model);
		}

		void DrawNavigationPanel(RectangleF dirtyRect)
		{
			using (var g = new LJD.Graphics ()) 
				drawing.Value.DrawNavigationPanel (g, MakeViewMetrics(), model);
		}

		void ActivitiesViewScrollWheel(NSEvent evt)
		{
			var viewMetrics = MakeViewMetrics ();
			model.OnScrollWheel (-evt.ScrollingDeltaX / (double)viewMetrics.ActivitiesViewWidth);
			if (evt.ScrollingDeltaY != 0)
			{
				if ((evt.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
				{
					var pt = activitiesView.ConvertPointFromView(evt.LocationInWindow, null);
					model.OnMouseZoom(pt.X / (double)viewMetrics.ActivitiesViewWidth, (int)(-evt.DeltaY * 100));
				}
				else if (vertScroller.Enabled)
				{
					if (evt.HasPreciseScrollingDeltas)
						vertScroller.DoubleValue -= (evt.ScrollingDeltaY / GetVertScrollerValueRange(viewMetrics));
					else
						vertScroller.DoubleValue -= (Math.Sign(evt.ScrollingDeltaY) * 30d / GetVertScrollerValueRange(viewMetrics));
				}
			}
		}
	
		void ActivitiesViewMagnify(NSEvent evt)
		{
			var viewMetrics = MakeViewMetrics ();
			var pt = activitiesView.ConvertPointFromView(evt.LocationInWindow, null);
			model.OnGestureZoom(pt.X / (double)viewMetrics.ActivitiesViewWidth, evt.Magnification);
		}

		void EnsureNibLoadeded ()
		{
			Window.GetHashCode ();
		}

		void SetCursor (CursorType cursor)
		{
			if (cursor == CursorType.Hand)
				NSCursor.PointingHandCursor.Set ();
			else if (cursor == CursorType.SizeAll)
				NSCursor.OpenHandCursor.Set ();
			else if (cursor == CursorType.SizeWE)
				NSCursor.ResizeRightCursor.Set ();
			else
				NSCursor.ArrowCursor.Set ();
		}

		void SetActivitiesCursor (NSEvent e)
		{
			var pt = activitiesView.ConvertPointFromView (e.LocationInWindow, null).ToPoint ();
			SetCursor(MakeViewMetrics ().GetActivitiesPanelCursor (pt, model, () => new LJD.Graphics ()));
		}

		void SetNavigationCursor (NSEvent e)
		{
			var pt = navigatorView.ConvertPointFromView (e.LocationInWindow, null).ToPoint ();
			SetCursor (MakeViewMetrics ().GetNavigationPanelCursor (pt, model));
		}

		void UpdateVertScroller ()
		{
			var viewMetrics = MakeViewMetrics ();
			float contentSize = model.ActivitiesCount * viewMetrics.LineHeight;
			float windowSize = viewMetrics.ActivitiesViewHeight - viewMetrics.RulersPanelHeight;
			var enableScroller = model.ActivitiesCount > 0 && contentSize > windowSize;
			vertScroller.Enabled = enableScroller;
			vertScroller.Hidden = !enableScroller;
			if (enableScroller) {
				vertScroller.KnobProportion = windowSize / contentSize;
			} else {
				vertScroller.DoubleValue = 0;
			}
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

		void SetActivitiesToolTip (NSEvent e)
		{
			var toolTip = model.OnToolTip (new HitTestToken (activitiesView, e));
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