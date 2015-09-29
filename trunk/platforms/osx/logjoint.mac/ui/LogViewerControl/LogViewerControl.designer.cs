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
	[Register ("LogViewerControl")]
	partial class LogViewerControl
	{
		[Outlet]
		LogJoint.UI.LogViewerControl innerView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView view { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (view != null) {
				view.Dispose ();
				view = null;
			}

			if (innerView != null) {
				innerView.Dispose ();
				innerView = null;
			}
		}
	}
}
