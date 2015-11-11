using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using LogJoint.UI;

namespace LogJoint
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowAdapter mainWindowAdapter;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			mainWindowAdapter = new MainWindowAdapter();
			var window = mainWindowAdapter.Window; // get property to force loading of window's nib
			if (Environment.GetCommandLineArgs().FirstOrDefault(arg => arg == "--touch", null) != null)
				NSApplication.SharedApplication.Terminate(this);
			else
				window.MakeKeyAndOrderFront (this);
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return true;
		}

		[Export("OnAboutDialogMenuClicked:")]
		void OnAboutDialogMenuClicked()
		{
			mainWindowAdapter.OnAboutDialogMenuClicked();
		}

		[Export("OnOpenRecentMenuClicked:")]
		void OnOpenRecentMenuClicked()
		{
			mainWindowAdapter.OnOpenRecentMenuClicked();
		}

	}
}

