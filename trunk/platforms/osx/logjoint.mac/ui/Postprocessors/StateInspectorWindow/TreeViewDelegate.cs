using System;
using AppKit;
using Foundation;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeViewDelegate: NSOutlineViewDelegate
	{
		public StateInspectorWindowController owner;

		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			TreeNodeView view = (TreeNodeView)outlineView.MakeView ("view", this);
			if (view == null)
				view = new TreeNodeView () {
					owner = owner,
					Identifier = "view",
					Menu = new NSMenu() { Delegate = owner.GetContextMenuDelegate() }
				};
			view.Update((Node)item);
			return view;
		}

		public override void ItemWillExpand (NSNotification notification)
		{
			
		}

		public override void SelectionDidChange (NSNotification notification)
		{
			owner.ViewModel.OnSelectedNodesChanged ();
		}

		[Export("outlineView:rowViewForItem:")]
		public override NSTableRowView RowViewForItem(NSOutlineView outlineView, NSObject item)
		{
			TreeRowView view = (TreeRowView)outlineView.MakeView ("row", this);
			if (view == null)
				view = new TreeRowView () {
				owner = owner,
				Identifier = "row"
			};
			view.Update ((Node)item);
			return view;
		}
	};
}

