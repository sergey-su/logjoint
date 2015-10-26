using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace logjoint.graphics.test
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		GraphicsTestMainWindowController mainWindowController;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			mainWindowController = new GraphicsTestMainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}
	}
}

