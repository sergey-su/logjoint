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
	[Register ("TestFormatDialogController")]
	partial class TestFormatDialogController
	{
		[Outlet]
		AppKit.NSTextField iconLabel { get; set; }

		[Outlet]
		AppKit.NSView logViewerPlaceholder { get; set; }

		[Outlet]
		AppKit.NSTextField statusLabel { get; set; }

		[Action ("OnCloseClicked:")]
		partial void OnCloseClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (logViewerPlaceholder != null) {
				logViewerPlaceholder.Dispose ();
				logViewerPlaceholder = null;
			}

			if (iconLabel != null) {
				iconLabel.Dispose ();
				iconLabel = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}
		}
	}
}
