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
		Presenters.Postprocessing.IPostprocessorOutputForm
	{
		readonly TagsListViewController tagsListController;
		readonly QuickSearchTextBoxAdapter quickSearchTextBox;
		readonly ToastNotificationsViewAdapter toastNotifications;
		IViewModel viewModel;
		Resources resources;
		DrawingUtils drawingUtils;
		SizeF scrollMaxValues;
		ReadonlyRef<Size> arrowsAreaSize = new ReadonlyRef<Size>();

		public SequenceDiagramWindowController () :
			base ("SequenceDiagramWindow")
		{
			tagsListController = new TagsListViewController ();
			quickSearchTextBox = new QuickSearchTextBoxAdapter ();
			toastNotifications = new ToastNotificationsViewAdapter ();

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

			rolesCaptionsView.BackgroundColor = NSColor.TextBackground;
			rolesCaptionsView.OnPaint = PaintRolesCaptionsView;

			arrowsView.BackgroundColor = NSColor.TextBackground;
			arrowsView.OnPaint = PaintArrowsView;
			arrowsView.OnMagnify = ArrowsViewMagnify;
			arrowsView.OnMouseDown = ArrowsViewMouseDown;
			arrowsView.OnMouseUp = ArrowsViewMouseUp;
			arrowsView.OnMouseDragged = ArrowsViewMouseDrag;
			arrowsView.OnScrollWheel = ArrowsViewScrollWheel;
			NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, 
				ns => {
					arrowsAreaSize = new ReadonlyRef<Size>(
						arrowsView.Frame.Size.ToSizeF().ToSize());
					viewModel?.ChangeNotification?.Post();
				}, arrowsView);

			leftPanelView.BackgroundColor = NSColor.TextBackground;
			leftPanelView.OnPaint = PaintLeftPanel;
			leftPanelView.OnMouseDown = LeftPanelMouseDown;

			vertScroller.Action = new Selector ("OnVertScrollChanged");
			vertScroller.Target = this;
			horzScroller.Action = new Selector ("OnHorzScrollChanged");
			horzScroller.Target = this;

			arrowDetailsLink.LinkClicked = (s, e) => viewModel.OnTriggerClicked (e.Link.Tag);
			arrowDetailsLink.BackgroundColor = NSColor.TextBackground;

			Window.InitialFirstResponder = arrowsView;

			Window.WillClose += (s, e) => viewModel.OnWindowHidden ();
		}

		void IView.SetViewModel (IViewModel viewModel)
		{
			this.EnsureCreated();

			this.viewModel = viewModel;

			var fontSz = (NSFont.SystemFontSize + NSFont.SmallSystemFontSize) / 2f;
			this.resources = new Resources (
				viewModel,
				NSFont.SystemFontOfSize (NSFont.SystemFontSize).FamilyName,
				(float)fontSz)
			{
				BookmarkImage = new LJD.Image (NSImage.ImageNamed ("Bookmark.png")),
				FocusedMessageImage = new LJD.Image (NSImage.ImageNamed ("FocusedMsgSlave.png")),
				FocusedMsgSlaveVert = new LJD.Image (NSImage.ImageNamed ("FocusedMsgSlaveVert.png"))
			};

			this.drawingUtils = new DrawingUtils(viewModel, resources);

			var notificationsIconUpdater = Updaters.Create(
				() => viewModel.IsNotificationsIconVisibile,
				value => activeNotificationsButton.Hidden = !value
			);

			var updateCurrentArrowControls = Updaters.Create(
				() => viewModel.CurrentArrowInfo,
				value => {
					arrowNameTextField.StringValue = value.Caption;
					arrowDetailsLink.StringValue = value.DescriptionText;
					arrowDetailsLink.Links = (value.DescriptionLinks ?? Enumerable.Empty<Tuple<object, int, int>>())
						.Select(l => new NSLinkLabel.Link(l.Item2, l.Item3, l.Item1)).ToArray();
				}
			);

			var updateCollapseResponsesCheckbox = Updaters.Create (
				() => viewModel.IsCollapseResponsesChecked,
				value => collapseResponsesCheckbox.State = value ? NSCellStateValue.On : NSCellStateValue.Off
			);

			var updateCollapseRoleInstancesCheckbox = Updaters.Create(
				() => viewModel.IsCollapseRoleInstancesChecked,
				value => collapseRoleInstancesCheckbox.State = value ? NSCellStateValue.On : NSCellStateValue.Off);

			var scrollBarsUpdater = Updaters.Create (
				() => viewModel.ScrollInfo,
				value => UpdateScrollBars (value.vMax, value.vChange, value.vValue,
					value.hMax, value.hChange, value.hValue)
			);

			var invalidateViews = Updaters.Create (
				() => viewModel.RolesDrawInfo,
				() => viewModel.ArrowsDrawInfo,
				(_1, _2) => {
					rolesCaptionsView.NeedsDisplay = true;
					arrowsView.NeedsDisplay = true;
					leftPanelView.NeedsDisplay = true;
				}
			);

			viewModel.ChangeNotification.CreateSubscription(() => {
				notificationsIconUpdater();
				updateCurrentArrowControls();
				updateCollapseResponsesCheckbox ();
				updateCollapseRoleInstancesCheckbox ();
				scrollBarsUpdater ();
				invalidateViews ();
			});
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

		void UpdateScrollBars (int vMax, int vChange, int vValue, int hMax, int hChange, int hValue)
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

		ReadonlyRef<Size> IView.ArrowsAreaSize => arrowsAreaSize;

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

		Presenters.ToastNotificationPresenter.IView IView.ToastNotificationsView
		{
			get { return toastNotifications; }
		}

		void Presenters.Postprocessing.IPostprocessorOutputForm.Show ()
		{
			viewModel.OnWindowShown ();
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
			viewModel.OnScrolled(null, (int)(vertScroller.DoubleValue * scrollMaxValues.Height));
		}

		[Export("OnHorzScrollChanged")]
		void OnHorzScrollChanged()
		{
			viewModel.OnScrolled((int)(horzScroller.DoubleValue * scrollMaxValues.Width), null);
		}

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject sender)
		{
			viewModel.OnKeyDown (Key.Find);
		}

		partial void OnActiveNotificationButtonClicked (NSObject sender)
		{
			viewModel.OnActiveNotificationButtonClicked();
		}

		partial void OnCurrentTimeClicked (NSObject sender)
		{
			viewModel.OnFindCurrentTimeButtonClicked();
		}

		partial void OnNextBookmarkClicked (NSObject sender)
		{
			viewModel.OnNextBookmarkButtonClicked();
		}

		partial void OnNextUserActionClicked (NSObject sender)
		{
			viewModel.OnNextUserEventButtonClicked();
		}

		partial void OnPrevBookmarkClicked (NSObject sender)
		{
			viewModel.OnPrevBookmarkButtonClicked();
		}

		partial void OnPrevUserActionClicked (NSObject sender)
		{
			viewModel.OnPrevUserEventButtonClicked();
		}

		partial void OnCollapseResponsesClicked (NSObject sender)
		{
			viewModel.OnCollapseResponsesChange(collapseResponsesCheckbox.State == NSCellStateValue.On);
		}

		partial void OnCollapseRoleInstancesClicked (NSObject sender)
		{
			viewModel.OnCollapseRoleInstancesChange(collapseRoleInstancesCheckbox.State == NSCellStateValue.On);
		}

		internal void OnKeyEvent(Key key)
		{
			viewModel.OnKeyDown (key);
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
			viewModel.OnScrolled(scrolledX, scrolledY);
		}

		void ArrowsViewMagnify(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			viewModel.OnGestureZoom(pt, (float) evt.Magnification);
		}

		void ArrowsViewMouseDown(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			viewModel.OnArrowsAreaMouseDown(pt, evt.ClickCount == 2);
		}

		void ArrowsViewMouseUp(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			viewModel.OnArrowsAreaMouseUp(pt, GetModifiers(evt));
		}

		void ArrowsViewMouseDrag(NSEvent evt)
		{
			var pt = arrowsView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			viewModel.OnArrowsAreaMouseMove(pt);
		}

		void LeftPanelMouseDown(NSEvent evt)
		{
			var pt = leftPanelView.ConvertPointFromView(evt.LocationInWindow, null).ToPoint();
			viewModel.OnLeftPanelMouseDown(pt, evt.ClickCount == 2, GetModifiers(evt));
		}

		static Key GetModifiers(NSEvent evt)
		{
			return (evt.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0 ? 
				Key.MultipleSelectionModifier : Key.None;
		}
	}
}

