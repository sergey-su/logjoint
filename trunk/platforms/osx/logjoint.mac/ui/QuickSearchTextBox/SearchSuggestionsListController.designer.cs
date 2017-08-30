// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("SearchSuggestionsListController")]
	partial class SearchSuggestionsListController
	{
		[Outlet]
		AppKit.NSTableColumn displayNameColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn linkColumn { get; set; }

		[Outlet]
		AppKit.NSOutlineView list { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (list != null) {
				list.Dispose ();
				list = null;
			}

			if (displayNameColumn != null) {
				displayNameColumn.Dispose ();
				displayNameColumn = null;
			}

			if (linkColumn != null) {
				linkColumn.Dispose ();
				linkColumn = null;
			}
		}
	}
}
