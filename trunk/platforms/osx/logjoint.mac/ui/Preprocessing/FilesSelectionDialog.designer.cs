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
	[Register ("FilesSelectionDialogController")]
	partial class FilesSelectionDialogController
	{
		[Outlet]
		AppKit.NSTableView tableView { get; set; }

		[Action ("OnCancelButtonClicked:")]
		partial void OnCancelButtonClicked (Foundation.NSObject sender);

		[Action ("OnOpenButtonClicked:")]
		partial void OnOpenButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}
		}
	}

	[Register ("FilesSelectionDialog")]
	partial class FilesSelectionDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
