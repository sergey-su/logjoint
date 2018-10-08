// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	[Register ("TimeSeriesConfigWindowController")]
	partial class TimeSeriesConfigWindowController
	{
		[Outlet]
		AppKit.NSTableColumn checkedColumn { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel collapseAllLinkLabel { get; set; }

		[Outlet]
		AppKit.NSPopUpButton colorPopup { get; set; }

		[Outlet]
		AppKit.NSButton drawLineCheckbox { get; set; }

		[Outlet]
		AppKit.NSPopUpButton markerPopup { get; set; }

		[Outlet]
		AppKit.NSTableColumn nodeColumn { get; set; }

		[Outlet]
		AppKit.NSTextView nodeDescriptionTextView { get; set; }

		[Outlet]
		AppKit.NSOutlineView treeView { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel uncheckAllLinkLabel { get; set; }

		[Action ("onDrawLineChanged:")]
		partial void onDrawLineChanged (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (drawLineCheckbox != null) {
				drawLineCheckbox.Dispose ();
				drawLineCheckbox = null;
			}

			if (checkedColumn != null) {
				checkedColumn.Dispose ();
				checkedColumn = null;
			}

			if (collapseAllLinkLabel != null) {
				collapseAllLinkLabel.Dispose ();
				collapseAllLinkLabel = null;
			}

			if (colorPopup != null) {
				colorPopup.Dispose ();
				colorPopup = null;
			}

			if (markerPopup != null) {
				markerPopup.Dispose ();
				markerPopup = null;
			}

			if (nodeColumn != null) {
				nodeColumn.Dispose ();
				nodeColumn = null;
			}

			if (nodeDescriptionTextView != null) {
				nodeDescriptionTextView.Dispose ();
				nodeDescriptionTextView = null;
			}

			if (treeView != null) {
				treeView.Dispose ();
				treeView = null;
			}

			if (uncheckAllLinkLabel != null) {
				uncheckAllLinkLabel.Dispose ();
				uncheckAllLinkLabel = null;
			}
		}
	}

	[Register ("TimeSeriesConfigWindow")]
	partial class TimeSeriesConfigWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
