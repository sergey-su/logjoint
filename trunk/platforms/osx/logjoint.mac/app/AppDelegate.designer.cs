// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint
{
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		AppKit.NSMenuItem reportProblemMenuItem { get; set; }

		[Action ("OnAboutDialogMenuClicked:")]
		partial void OnAboutDialogMenuClicked (Foundation.NSObject sender);

		[Action ("OnNewDocumentClicked:")]
		partial void OnNewDocumentClicked (Foundation.NSObject sender);

		[Action ("OnOpenRecentMenuClicked:")]
		partial void OnOpenRecentMenuClicked (Foundation.NSObject sender);

		[Action ("OnReportProblemMenuItemClicked:")]
		partial void OnReportProblemMenuItemClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (reportProblemMenuItem != null) {
				reportProblemMenuItem.Dispose ();
				reportProblemMenuItem = null;
			}
		}
	}
}
