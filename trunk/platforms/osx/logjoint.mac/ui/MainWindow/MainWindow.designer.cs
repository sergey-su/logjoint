// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSView logViewerPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButtonCell myButtonClick { get; set; }

		[Action ("onButtonClicked:")]
		partial void onButtonClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (myButtonClick != null) {
				myButtonClick.Dispose ();
				myButtonClick = null;
			}

			if (logViewerPlaceholder != null) {
				logViewerPlaceholder.Dispose ();
				logViewerPlaceholder = null;
			}
		}
	}

	[Register ("MainWindow")]
	partial class MainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
