using System;
using AppKit;
using Foundation;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeDataSource: NSOutlineViewDataSource
	{
		public readonly Node root = new Node();

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			return GetNode(item).children.Count;
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			return GetNode(item).children[(int)childIndex];
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			return GetNode(item).children.Count > 0;
		}

		Node GetNode(NSObject item)
		{
			return item == null ? root : (Node)item;;
		}
	};
}

