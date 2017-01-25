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
			// this is the right place to register in NSAppleEventManager
			// doing so in ComponentsInitializer is too late
			CustomURLSchemaEventsHandler.Instance.Register();
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

		partial void OnAboutDialogMenuClicked(NSObject sender)
		{
			mainWindowAdapter.OnAboutDialogMenuClicked();
		}

		partial void OnOpenRecentMenuClicked(NSObject sender)
		{
			mainWindowAdapter.OnOpenRecentMenuClicked();
		}
		
		partial void OnReportProblemMenuItemClicked (NSObject sender)
		{
			mainWindowAdapter.OnReportProblemMenuItemClicked();
		}
	}
}