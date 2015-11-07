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
	[Register ("StatusPopupControlAdapter")]
	partial class StatusPopupControlAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSBox box { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField captionLabel { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel contentLinkLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (box != null) {
				box.Dispose ();
				box = null;
			}

			if (contentLinkLabel != null) {
				contentLinkLabel.Dispose ();
				contentLinkLabel = null;
			}

			if (captionLabel != null) {
				captionLabel.Dispose ();
				captionLabel = null;
			}
		}
	}

	[Register ("StatusPopupControl")]
	partial class StatusPopupControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
