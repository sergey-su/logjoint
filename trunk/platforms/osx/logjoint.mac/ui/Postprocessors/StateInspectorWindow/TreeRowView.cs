﻿using System;
using AppKit;
using CoreGraphics;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeRowView: NSTableRowView
	{
		public StateInspectorWindowController owner;
		public IObjectsTreeNode node;

		public void Update(IObjectsTreeNode n)
		{
			node = n;
			this.NeedsDisplay = true;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			bool isSelected = node.IsSelected;
			bool isDarkMode = owner.ViewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
			var paintInfo = owner.ViewModel.PaintNode(node, false);
			if (isSelected) {
				NSColor.SelectedMenuItem.SetFill ();
			}
			else {
				switch (paintInfo.Coloring) {
				case NodeColoring.Alive:
					if (isDarkMode)
						NSColor.FromDeviceRgba (0.0f, 0.20f, 0.0f, 1f).SetFill ();
					else
						NSColor.FromDeviceRgba (0.74f, 0.93f, 0.74f, 1f).SetFill ();
					break;
				case NodeColoring.Deleted:
					if (isDarkMode)
						NSColor.FromDeviceRgba (0.30f, 0.30f, 0.30f, 1f).SetFill ();
					else
						NSColor.FromDeviceRgba (0.90f, 0.90f, 0.98f, 1f).SetFill ();
					break;
				default:
					NSColor.TextBackground.SetFill ();
					break;
				}
			}
			NSBezierPath.FillRect (dirtyRect);
			// paintInfo.DrawFocusedMsgMark todo
		}
	};
}

