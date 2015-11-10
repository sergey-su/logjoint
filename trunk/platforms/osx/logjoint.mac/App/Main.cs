using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	class MainClass
	{
		static void Main (string[] args)
		{
			if (args.Length == 1 && args[0] == "--toutch")
				return;
			
			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

