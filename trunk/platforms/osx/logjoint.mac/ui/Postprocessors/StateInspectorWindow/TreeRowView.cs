using System;
using AppKit;
using System.Drawing;
using CoreGraphics;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeRowView: NSTableRowView
	{
		public StateInspectorWindowController owner;
		public Node node;

		public void Update(Node n)
		{
			node = n;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			bool isSelected = owner.TreeView.IsRowSelected (owner.TreeView.RowForItem (node));
			var paintInfo = owner.EventsHandler.OnPaintNode(node.ToNodeInfo(), false);
			if (isSelected) {
				NSColor.SelectedMenuItem.SetFill ();
			}
			else {
				switch (paintInfo.Coloring) {
				case NodeColoring.Alive:
					NSColor.FromDeviceRgba (0.74f, 0.93f, 0.74f, 1f).SetFill ();
					break;
				case NodeColoring.Deleted:
					NSColor.FromDeviceRgba (0.90f, 0.90f, 0.98f, 1f).SetFill ();
					break;
				default:
					NSColor.White.SetFill ();
					break;
				}
			}
			NSBezierPath.FillRect (dirtyRect);
			// paintInfo.DrawFocusedMsgMark todo
		}
	};
}

