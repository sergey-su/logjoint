// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace logjoint.graphics.test
{
	[Register ("GraphicsTestMainWindowController")]
	partial class GraphicsTestMainWindowController
	{
		[Outlet]
		MonoMac.Foundation.NSObject view { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (view != null) {
				view.Dispose ();
				view = null;
			}
		}
	}

	[Register ("GraphicsTestMainWindow")]
	partial class GraphicsTestMainWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
