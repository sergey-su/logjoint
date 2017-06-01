// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	[Register ("TimelineWindowController")]
	partial class TimelineWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSButton activeNotificationsButton { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView activitiesView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel activityDetailsLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel activityLogSourceLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField activityNameTextField { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView captionsView { get; set; }

		[Outlet]
		LogJoint.UI.NSCustomizableView navigatorView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView searchTextBoxPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView tagsSelectorPlacefolder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScroller vertScroller { get; set; }

		[Action ("OnActiveNotificationsButtonClicked:")]
		partial void OnActiveNotificationsButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnNextUserActionClicked:")]
		partial void OnNextUserActionClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnPing:")]
		partial void OnPing (MonoMac.Foundation.NSObject sender);

		[Action ("OnPrevUserActionClicked:")]
		partial void OnPrevUserActionClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnZoomInClicked:")]
		partial void OnZoomInClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnZoomOutClicked:")]
		partial void OnZoomOutClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (activitiesView != null) {
				activitiesView.Dispose ();
				activitiesView = null;
			}

			if (activityDetailsLabel != null) {
				activityDetailsLabel.Dispose ();
				activityDetailsLabel = null;
			}

			if (activityLogSourceLabel != null) {
				activityLogSourceLabel.Dispose ();
				activityLogSourceLabel = null;
			}

			if (activityNameTextField != null) {
				activityNameTextField.Dispose ();
				activityNameTextField = null;
			}

			if (captionsView != null) {
				captionsView.Dispose ();
				captionsView = null;
			}

			if (navigatorView != null) {
				navigatorView.Dispose ();
				navigatorView = null;
			}

			if (searchTextBoxPlaceholder != null) {
				searchTextBoxPlaceholder.Dispose ();
				searchTextBoxPlaceholder = null;
			}

			if (tagsSelectorPlacefolder != null) {
				tagsSelectorPlacefolder.Dispose ();
				tagsSelectorPlacefolder = null;
			}

			if (vertScroller != null) {
				vertScroller.Dispose ();
				vertScroller = null;
			}

			if (activeNotificationsButton != null) {
				activeNotificationsButton.Dispose ();
				activeNotificationsButton = null;
			}
		}
	}

	[Register ("TimelineWindow")]
	partial class TimelineWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
