// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("TimelinePanelControlAdapter")]
	partial class TimelinePanelControlAdapter
	{
		[Outlet]
		AppKit.NSButton moveDownButton { get; set; }

		[Outlet]
		AppKit.NSButton moveUpButton { get; set; }

		[Outlet]
		AppKit.NSButton resetZoomButton { get; set; }

		[Outlet]
		AppKit.NSView timelineControlPlaceholder { get; set; }

		[Outlet]
		AppKit.NSButton zoomInButton { get; set; }

		[Outlet]
		AppKit.NSButton zoomOutButton { get; set; }

		[Action ("OnMoveDownClicked:")]
		partial void OnMoveDownClicked (Foundation.NSObject sender);

		[Action ("OnMoveUpClicked:")]
		partial void OnMoveUpClicked (Foundation.NSObject sender);

		[Action ("OnResetZoomClicked:")]
		partial void OnResetZoomClicked (Foundation.NSObject sender);

		[Action ("OnZoomInClicked:")]
		partial void OnZoomInClicked (Foundation.NSObject sender);

		[Action ("OnZoomOutClicked:")]
		partial void OnZoomOutClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (timelineControlPlaceholder != null) {
				timelineControlPlaceholder.Dispose ();
				timelineControlPlaceholder = null;
			}

			if (zoomInButton != null) {
				zoomInButton.Dispose ();
				zoomInButton = null;
			}

			if (zoomOutButton != null) {
				zoomOutButton.Dispose ();
				zoomOutButton = null;
			}

			if (resetZoomButton != null) {
				resetZoomButton.Dispose ();
				resetZoomButton = null;
			}

			if (moveDownButton != null) {
				moveDownButton.Dispose ();
				moveDownButton = null;
			}

			if (moveUpButton != null) {
				moveUpButton.Dispose ();
				moveUpButton = null;
			}
		}
	}

	[Register ("TimelinePanelControl")]
	partial class TimelinePanelControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
