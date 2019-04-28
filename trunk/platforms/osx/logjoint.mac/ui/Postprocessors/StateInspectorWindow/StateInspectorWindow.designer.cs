// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	[Register ("StateInspectorWindowController")]
	partial class StateInspectorWindowController
	{
		[Outlet]
		AppKit.NSTextField currentTimeLabel { get; set; }

		[Outlet]
		AppKit.NSButton findCurrentPositionInStateHistoryButton { get; set; }

		[Outlet]
		AppKit.NSTableColumn historyItemDecorationColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn historyItemTextColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn historyItemTimeColumn { get; set; }

		[Outlet]
		LogJoint.UI.Postprocessing.StateInspector.StateInspectorPropertiesView propertiesView { get; set; }

		[Outlet]
		AppKit.NSTableColumn propKeyColumn { get; set; }

		[Outlet]
		AppKit.NSTableColumn propValueColumn { get; set; }

		[Outlet]
		AppKit.NSTableView stateHistoryView { get; set; }

		[Outlet]
		AppKit.NSOutlineView treeView { get; set; }

		[Action ("OnFindCurrentPositionInStateHistory:")]
		partial void OnFindCurrentPositionInStateHistory (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (findCurrentPositionInStateHistoryButton != null) {
				findCurrentPositionInStateHistoryButton.Dispose ();
				findCurrentPositionInStateHistoryButton = null;
			}

			if (currentTimeLabel != null) {
				currentTimeLabel.Dispose ();
				currentTimeLabel = null;
			}

			if (historyItemDecorationColumn != null) {
				historyItemDecorationColumn.Dispose ();
				historyItemDecorationColumn = null;
			}

			if (historyItemTextColumn != null) {
				historyItemTextColumn.Dispose ();
				historyItemTextColumn = null;
			}

			if (historyItemTimeColumn != null) {
				historyItemTimeColumn.Dispose ();
				historyItemTimeColumn = null;
			}

			if (propertiesView != null) {
				propertiesView.Dispose ();
				propertiesView = null;
			}

			if (propKeyColumn != null) {
				propKeyColumn.Dispose ();
				propKeyColumn = null;
			}

			if (propValueColumn != null) {
				propValueColumn.Dispose ();
				propValueColumn = null;
			}

			if (stateHistoryView != null) {
				stateHistoryView.Dispose ();
				stateHistoryView = null;
			}

			if (treeView != null) {
				treeView.Dispose ();
				treeView = null;
			}
		}
	}

	[Register ("StateInspectorWindow")]
	partial class StateInspectorWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
