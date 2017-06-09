// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("LogViewerControl")]
	partial class LogViewerControl
	{
		[Outlet]
		LogJoint.UI.LogViewerControl innerView { get; set; }

		[Outlet]
		AppKit.NSScrollView scrollView { get; set; }

		[Outlet]
		AppKit.NSScroller vertScroller { get; set; }

		[Outlet]
		AppKit.NSView view { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (innerView != null) {
				innerView.Dispose ();
				innerView = null;
			}

			if (view != null) {
				view.Dispose ();
				view = null;
			}

			if (scrollView != null) {
				scrollView.Dispose ();
				scrollView = null;
			}

			if (vertScroller != null) {
				vertScroller.Dispose ();
				vertScroller = null;
			}
		}
	}
}
