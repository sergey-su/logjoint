// WARNING
//
// This file has been generated automatically by Xamarin Studio Community to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	[Register ("StateInspectorWindowController")]
	partial class StateInspectorWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSTextField currentTimeLabel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn historyItemDecorationColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn historyItemTextColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn historyItemTimeColumn { get; set; }

		[Outlet]
		LogJoint.UI.Postprocessing.StateInspector.StateInspectorPropertiesView propertiesView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn propKeyColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableColumn propValueColumn { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView stateHistoryView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSOutlineView treeView { get; set; }

		[Action ("OnFindCurrentPositionInStateHistory:")]
		partial void OnFindCurrentPositionInStateHistory (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
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
