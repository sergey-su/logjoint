using System;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public static class UIUtils
	{
		public static void MoveToPlaceholder(this NSView customControlView, NSView placeholder)
		{
			placeholder.AddSubview (customControlView);
			var placeholderSize = placeholder.Frame.Size;
			customControlView.Frame = new System.Drawing.RectangleF(0, 0, placeholderSize.Width, placeholderSize.Height);
			customControlView.AutoresizingMask = 
				NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		}
	}
}

