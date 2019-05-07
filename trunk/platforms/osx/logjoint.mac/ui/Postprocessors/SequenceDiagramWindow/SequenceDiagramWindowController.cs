using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer;
using LJD = LogJoint.Drawing;
using ObjCRuntime;
using LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	public partial class SequenceDiagramWindowController : 
		AppKit.NSWindowController,
		IView,
		Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputForm
	{
		readonly TagsListViewController tagsListController;
		readonly QuickSearchTextBoxAdapter quickSearchTextBox;
		readonly ToastNotificationsViewAdapter toastNotifications;
		IViewEvents eventsHandler;
		Resources resources;
		DrawingUtils drawingUtils;
		SizeF scrollMaxValues;

		public SequenceDiagramWindowController () :
			base ("SequenceDiagramWindow")
		{
			tagsListController = new TagsListViewController ();
			quickSearchTextBox = new QuickSearchTextBoxAdapter ();
			toastNotifications = new ToastNotificationsViewAdapter ();

			var fontSz = (NSFont.SystemFontSize + NSFont.SmallSystemFontSize) / 2f;
			this.resources = new Resources (
				NSFont.SystemFontOfSize(NSFont.SystemFontSize).FamilyName, (float) fontSz);
			
			this.resources.BookmarkImage = new LJD.Image(NSImage.ImageNamed("Bookmark.png"));
			this.resources.FocusedMessageImage = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlave.png"));
			this.resources.FocusedMsgSlaveVert = new LJD.Image(NSImage.ImageNamed("FocusedMsgSlaveVert.png"));
		
			Window.owner = this;
		}

		new SequenceDiagramWindow Window 
		{
			get { return (SequenceDiagramWindow)base.Window; }
		}

		static void PlaceToastNotificationsView(NSView toastNotificationsView, NSView parent)
		{
			parent.AddSubview (toastNotificationsView);
			toastNotificationsView.TranslatesAutoresizingMaskIntoConstraints = false;
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Trailing, 1f, 2f));
			parent.AddConstraint(NSLayoutConstraint.Create(
				toastNotificationsView, NSLayoutAttribute.Top, NSLayoutRelation.Equal,
				parent, NSLayoutAttribute.Top, 1f, 2f));
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			tagsListController.View.MoveToPlaceholder(tagsViewPlaceholder);
			quickSearchTextBox.View.MoveToPlaceholder(quickSearchPlaceholder);
			PlaceToastNotificationsView(toastNotifications.View, arrowsView);

			rolesCaptionsView.BackgroundColor = NSColor.White;
			rolesCaptionsView.OnPaint = PaintRolesCaptionsView;

			arrowsView.BackgroundColor = NSColor.White;
			arrowsView.OnPaint = PaintArrowsView;
			arrowsView.OnMagnify = ArrowsViewMagnify;
			arrowsView.OnMouseDown = ArrowsViewMouseDown;
			arrowsView.OnMouseUp = ArrowsViewMouseUp;
			arrowsView.OnMouseDragged = ArrowsViewMouseDrag;
			arrowsView.OnScrollWheel = ArrowsViewScrollWheel;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, 
				ns => { if (eventsHandler != null) eventsHandler.OnResized(); }, arrowsView);

			leftPanelView.BackgroundColor = NSColor.White;
			leftPanelView.OnPaint = PaintLeftPanel;
			leftPanelView.OnMouseDown = LeftPanelMouseDown;

			vertScroller.Action = new Selector ("OnVertScrollChanged");
			vertScroller.Target = this;
			horzScroller.Action = new Selector ("OnHorzScrollChanged");
			horzScroller.Target = this;

			arrowDetailsLink.LinkClicked = (s, e) => eventsHandler.OnTriggerClicked (e.Link.Tag);
			arrowDetailsLink.BackgroundColor = NSColor.White;

			Window.InitialFirstResponder = arrowsView;

			Window.WillClose += (s, e) => eventsHandler.OnWindowHidden ();
		}

		void IView.SetEventsHandler (IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			this.drawingUtils = new DrawingUtils(eventsHandler, resources);
		}

		ViewMetrics IView.GetMetrics ()
		{
			return new ViewMetrics()
			{
				MessageHeight = 22,
				NodeWidth = 200,
				ExecutionOccurrenceWidth = 8,
				ExecutionOccurrenceLevelOffset = 6,
				ParallelNonHorizontalArrowsOffset = 4,
				VScrollOffset = 60,
			};
		}

		void IView.Invalidate ()
		{
			rolesCaptionsView.NeedsDisplay = true;
			arrowsView.NeedsDisplay = true;
			leftPanelView.NeedsDisplay = true;
		}

		void IView.UpdateCurrentArrowControls (string caption, string descriptionText, IEnumerable<Tuple<object, int, int>> descriptionLinks)
		{
			arrowNameTextField.StringValue = caption;
			arrowDetailsLink.StringValue = descriptionText;
			arrowDetailsLink.Links = (descriptionLinks ?? Enumerable.Empty<Tuple<object, int, int>>())
				.Select(l => new NSLinkLabel.Link(l.Item2, l.Item3, l.Item1)).ToArray();
		}

		void IView.UpdateScrollBars (int vMax, int vChange, int vValue, int hMax, int hChange, int hValue)
		{
			Action<NSScroller, int, int, int> set = (scroll, max, change, value) =>
			{
				bool enableScroller = max > 0 && change < max;
				scroll.Enabled = enableScroller;
				if (enableScroller)
				{
					scroll.KnobProportion = (float)change/(float)max;
					var newValue = (double)value/(double)(max - change);
					if (Math.Abs(scroll.DoubleValue - newValue) > 0.01) 
					{
						// Change scroll position only when change is bigger 
						// than a threshold. This condition is true for
						// presenter-initiated changes such as scrolling to
						// a bookmark.
						// The condition is false when UpdateScrollBars is 
						// called during user-initiated scrolling.
						// OSX should be the only source of scroll position
						// to preserve scrolling smoothness.
						scroll.DoubleValue = newValue;
					}
				}
				else
				{
					scroll.DoubleValue = 0;
				}
			};
			set(horzScroller, hMax, hChange, hValue);
			set(vertScroller, vMax, vChange, vValue);
			scrollMaxValues = new SizeF (hMax - hChange, vMax - vChange);
		}

		int IView.ArrowsAreaWidth 
		{
			get { return (int) arrowsView.Frame.Width; }
		}

		int IView.ArrowsAreaHeight 
		{
			get { return (int) arrowsView.Frame.Height; }
		}

		int IView.RolesCaptionsAreaHeight 
		{
			get { return (int) rolesCaptionsView.Frame.Height; }
		}

		Presenters.TagsList.IView IView.TagsListView 
		{
			get { return tagsListController; }
		}

		LogJoint.UI.Presenters.QuickSearchTextBox.IView IView.QuickSearchTextBox
		{
			get { return quickSearchTextBox; }
		}

		void IView.PutInputFocusToArrowsArea()
		{
			arrowsView.Window.MakeFirstResponder(arrowsView);
		}

		bool IView.IsCollapseResponsesChecked
		{
			get { return collapseResponsesCheckbox.State == NSCellStateValue.On; }
			set { collapseResponsesCheckbox.State = value ? NSCellStateValue.On : NSCellStateValue.Off; }
		}

		bool IView.IsCollapseRoleInstancesChecked
		{
			get { return collapseRoleInstancesCheckbox.State == NSCellStateValue.On; }
			set { collapseRoleInstancesCheckbox.State = value ? NSCellStateValue.On : NSCellStateValue.Off; }
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
			eventsHandler.OnWindowShown ();
			Window.MakeKeyAndOrderFront (null);
		}

		void PaintRolesCaptionsView(RectangleF dyrtyRect)
		{
			if (drawingUtils == null)
				return;
			using (var g = new LJD.Graphics())
				drawingUtils.DrawRoleCaptions(g);
		}

		void PaintArrowsView(RectangleF dyrtyRect)
		{			
			if (drawingUtils == null)
				return;

			using (var g = new LJD.Graphics())
			{
				drawingUtils.DrawArrowsView(
					g,
					arrowsView.Frame.Size.ToSizeF().ToSize (),
					r => {}//ControlPaint.DrawFocusRectangle(e.Graphics, r) todo
				);
			}
		}

		void PaintLeftPanel(RectangleF dyrtyRect)
		{
			if (drawingUtils == null)
				return;
			using (var g = new LJD.Graphics())
				drawingUtils.DrawLeftPanelView(g, 
					//leftPanel.PointToClient(arrowsPanel.PointToScreen(new Point())), 
					new Point(),// todo
					leftPanelView.Frame.Size.ToSizeF().ToSize ());
		}

		[Export("OnVertScrollChanged")]
		void OnVertScrollChanged()
		{
			eventsHandler.OnScrolled(null, (int)(vertScroller.DoubleValue * scrollMaxValues.Height));
		}

		[Export("OnHorzScrollChanged")]
		void OnHorzScrollChanged()
		{
			eventsHandler.OnScrolled((int)(horzScroller.DoubleValue * scrollMaxValues.Width), null);
		}

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject sender)
		{
			eventsHandler.OnKeyDown (Key.Find);
		}

		partial void OnActiveNotificationButtonClicked (NSObject sender)
		{
			eventsHandler.OnActiveNotificationButtonClicked();
		}

		partial void OnCurrentTimeClicked (NSObject sender)
		{
			eventsHandler.OnFindCurrentTimeButtonClicked();
		}

		partial void OnNextBookmarkClicked (NSObject sender)
		{
			eventsHandler.OnNextBookmarkButtonClicked();
		}

		partial void OnNextUserActionClicked (NSObject sender)
		{
			eventsHandler.OnNextUserEventButtonClicked();
		}

		partial void OnPrevBookmarkClicked (NSObject sender)
		{
			eventsHandler.OnPrevBookmarkButtonClicked();
		}

		partial void OnPrevUserActionClicked (NSObject sender)
		{
			eventsHandler.OnPrevUserEventButtonClicked();
		}

		partial void OnCollapseResponsesClicked (NSObject sender)
		{
			eventsHandler.OnCollapseResponsesChanged();
		}

		partial void OnCollapseRoleInstancesClicked (NSObject sender)
		{
			eventsHandler.OnCollapseRoleInstancesChanged();
		}

		internal void OnKeyEvent(Key key)
		{
			eventsHandler.OnKeyDown (key);
		}

		internal void OnCancelOperation()
		{
			Window.Close ();
		}

		void ArrowsViewScrollWheel(NSEvent evt)
		{
			int? scrolledX = null;
			int? scrolledY = null;
			if (evt.ScrollingDeltaX != 0 && horzScroller.Enabled)
			{
				horzScroller.DoubleValue -= (evt.ScrollingDeltaX / scrollMaxValues.Width);
				scrolledX = (int)(horzScroller.DoubleValue * scrollMaxValues.Width);
			}
			if (evt.ScrollingDeltaY != 0 && vertScroller.Enabled)
			{
				if (evt.HasPreciseScrollingDeltas)
					vertScroller.DoubleValue -= (evt.ScrollingDeltaY / scrollMaxValues.Height);
				else
					vertScroller.DoubleValue -= (Math.Sign(evt.ScrollingDeltaY) * 30d / scrollMaxValues.Height);
				scrolledY = (int)(vertScroller.DoubleValue * scrollMaxValues.Height);
			}
			eventsHandler.OnScrolled(scrolledX, scrolledY);
		}

		void ArrowsViewMagnify(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			eventsHandler.OnGestureZoom(pt, (float) evt.Magnification);
		}

		void ArrowsViewMouseDown(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			eventsHandler.OnArrowsAreaMouseDown(pt, evt.ClickCount == 2);
		}

		void ArrowsViewMouseUp(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			eventsHandler.OnArrowsAreaMouseUp(pt, GetModifiers(evt));
		}

		void ArrowsViewMouseDrag(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			eventsHandler.OnArrowsAreaMouseMove(pt);
		}

		void LeftPanelMouseDown(NSEvent evt)
		{
			var pt = leftPanelView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			eventsHandler.OnLeftPanelMouseDown(pt, evt.ClickCount == 2, GetModifiers(evt));
		}

		static Key GetModifiers(NSEvent evt)
		{
			return (evt.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0 ? 
				Key.MultipleSelectionModifier : Key.None;
		}
	}
}

