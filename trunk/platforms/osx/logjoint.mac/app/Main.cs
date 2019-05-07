using System;
using Foundation;
using AppKit;
using ObjCRuntime;

namespace LogJoint.UI
{
	class MainClass
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}

