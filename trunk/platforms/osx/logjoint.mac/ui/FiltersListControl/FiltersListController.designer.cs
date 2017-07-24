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
	[Register ("FiltersListController")]
	partial class FiltersListController
	{
		[Outlet]
		AppKit.NSTableColumn checkboxColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn imageColumn { get; set; }

		[Outlet]
		AppKit.NSOutlineView listView { get; set; }

		[Outlet]
		AppKit.NSTableColumn textColumn { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (textColumn != null) {
				textColumn.Dispose ();
				textColumn = null;
			}

			if (listView != null) {
				listView.Dispose ();
				listView = null;
			}

			if (checkboxColumn != null) {
				checkboxColumn.Dispose ();
				checkboxColumn = null;
			}

			if (imageColumn != null) {
				imageColumn.Dispose ();
				imageColumn = null;
			}
		}
	}
}
