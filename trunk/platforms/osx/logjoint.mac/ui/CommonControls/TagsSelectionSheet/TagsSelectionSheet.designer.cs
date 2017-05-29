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
	[Register ("TagsSelectionSheet")]
	partial class TagsSelectionSheet
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel linkLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView table { get; set; }

		[Outlet]
		LogJoint.UI.TagsSelectionSheet window { get; set; }

		[Action ("OnCancelled:")]
		partial void OnCancelled (MonoMac.Foundation.NSObject sender);

		[Action ("OnConfirmed:")]
		partial void OnConfirmed (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (linkLabel != null) {
				linkLabel.Dispose ();
				linkLabel = null;
			}

			if (table != null) {
				table.Dispose ();
				table = null;
			}

			if (window != null) {
				window.Dispose ();
				window = null;
			}
		}
	}
}
