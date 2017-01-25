// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint
{
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		MonoMac.AppKit.NSMenuItem reportProblemMenuItem { get; set; }

		[Action ("OnAboutDialogMenuClicked:")]
		partial void OnAboutDialogMenuClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnOpenRecentMenuClicked:")]
		partial void OnOpenRecentMenuClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnReportProblemMenuItemClicked:")]
		partial void OnReportProblemMenuItemClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (reportProblemMenuItem != null) {
				reportProblemMenuItem.Dispose ();
				reportProblemMenuItem = null;
			}
		}
	}
}
