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
	[Register ("HistoryDialogController")]
	partial class HistoryDialogAdapter
	{
		[Outlet]
		MonoMac.AppKit.NSButton openButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSOutlineView outlineView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSScrollView outlineViewContainer { get; set; }

		[Outlet]
		MonoMac.AppKit.NSView quickSearchTextBoxPlaceholder { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTreeController treeController { get; set; }

		[Action ("OnCancelButtonClicked:")]
		partial void OnCancelButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnClearHistoryButtonClicked:")]
		partial void OnClearHistoryButtonClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnListDoubleClicked:")]
		partial void OnListDoubleClicked (MonoMac.Foundation.NSObject sender);

		[Action ("OnOpenButtonClicked:")]
		partial void OnOpenButtonClicked (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (openButton != null) {
				openButton.Dispose ();
				openButton = null;
			}

			if (outlineView != null) {
				outlineView.Dispose ();
				outlineView = null;
			}

			if (outlineViewContainer != null) {
				outlineViewContainer.Dispose ();
				outlineViewContainer = null;
			}

			if (quickSearchTextBoxPlaceholder != null) {
				quickSearchTextBoxPlaceholder.Dispose ();
				quickSearchTextBoxPlaceholder = null;
			}

			if (treeController != null) {
				treeController.Dispose ();
				treeController = null;
			}
		}
	}

	[Register ("HistoryDialog")]
	partial class HistoryDialog
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
