// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("BookmarksListControlAdapter")]
	partial class BookmarksListControlAdapter
	{
		[Outlet]
		AppKit.NSTableColumn currentPositionIndicatorColumn { get; set; }

		[Outlet]
		AppKit.NSClipView tableContainer { get; set; }

		[Outlet]
		AppKit.NSTableView tableView { get; set; }

		[Outlet]
		AppKit.NSTableColumn textColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn timeDeltaColumn { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (tableContainer != null) {
				tableContainer.Dispose ();
				tableContainer = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (textColumn != null) {
				textColumn.Dispose ();
				textColumn = null;
			}

			if (timeDeltaColumn != null) {
				timeDeltaColumn.Dispose ();
				timeDeltaColumn = null;
			}

			if (currentPositionIndicatorColumn != null) {
				currentPositionIndicatorColumn.Dispose ();
				currentPositionIndicatorColumn = null;
			}
		}
	}

	[Register ("BookmarksListControl")]
	partial class BookmarksListControl
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
