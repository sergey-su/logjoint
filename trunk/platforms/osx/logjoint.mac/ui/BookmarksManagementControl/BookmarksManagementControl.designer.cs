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
	[Register ("BookmarksManagementControlAdapter")]
	partial class BookmarksManagementControlAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSView bookmarksListPlaceholder { get; set; }

		[Action ("OnAddBookmarkButtonClicked:")]
		partial void OnAddBookmarkButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnRemoveBookmarkButtonClicked:")]
		partial void OnRemoveBookmarkButtonClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (bookmarksListPlaceholder != null) {
				bookmarksListPlaceholder.Dispose ();
				bookmarksListPlaceholder = null;
			}
		}
	}

	[Register ("BookmarksManagementControl")]
	partial class BookmarksManagementControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
