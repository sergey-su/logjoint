using System;
using AppKit;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	enum NodeOpType
	{
		ExpandAll,
		Collapse,
		InvalidateNodeView
	};

	class NodeOp
	{
		public readonly NodeOpType Type;
		public readonly Node Target;

		public NodeOp(NodeOpType t, Node target)
		{
			Type = t;
			Target = target;
		}

		public void Execute(NSOutlineView treeView, bool playingDelayedOps)
		{
			switch (Type) {
			case NodeOpType.ExpandAll:
				treeView.ExpandItem (Target, true);
				break;
			case NodeOpType.Collapse:
				treeView.CollapseItem (Target, false);
				break;
			case NodeOpType.InvalidateNodeView:
				if (!playingDelayedOps)
					treeView.NeedsDisplay = true;
				break;
			}
		}
	};
}

