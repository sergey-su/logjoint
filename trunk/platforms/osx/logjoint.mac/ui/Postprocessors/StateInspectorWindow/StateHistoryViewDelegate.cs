using System;
using AppKit;
using Foundation;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class StateHistoryViewDelegate: NSTableViewDelegate
	{
		public StateInspectorWindowController owner;

		public override bool ShouldSelectRow (NSTableView tableView, nint row)
		{
			return true;
		}

		public override NSTableRowView CoreGetRowView(NSTableView tableView, nint row)
		{
			return new StateHistroryTableRowView() { owner = owner, row = (int)row };
		}

		public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			if (tableColumn == owner.HistoryItemTimeColumn)
			{
				var view = MakeTextField (tableView, "timeView");
				view.StringValue = owner.StateHistoryDataSource.data[(int)row].Time;
				return view;
			}
			else if (tableColumn == owner.HistoryItemTextColumn)
			{
				var view = MakeTextField (tableView, "messageView");
				view.StringValue = owner.StateHistoryDataSource.data[(int)row].Message;
				return view;
			}

			return null;
		}

		NSTextField MakeTextField (NSTableView tableView, string viewId)
		{
			NSTextField view = (NSTextField)tableView.MakeView (viewId, this);
			if (view == null) {
				view = new NSTextField () {
					Identifier = viewId,
					Editable = false,
					Selectable = false,
					Bordered = false,
					BackgroundColor = NSColor.Clear
				};
				view.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
			}
			return view;
		}

		public override void SelectionDidChange(NSNotification notification)
		{
			if (!owner.IsUpdatingStateHistory)
				owner.ViewModel.OnChangeHistorySelectionChanged();
		}
	}
}

