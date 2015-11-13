using System;
using MonoMac.AppKit;
using System.Drawing;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI
{
	public static class UIUtils
	{
		public static void MoveToPlaceholder(this NSView customControlView, NSView placeholder)
		{
			placeholder.AddSubview (customControlView);
			var placeholderSize = placeholder.Frame.Size;
			customControlView.Frame = new System.Drawing.RectangleF(0, 0, placeholderSize.Width, placeholderSize.Height);
			customControlView.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
		}

		public static void InvalidateCursorRects(this NSView view)
		{
			if (view.Window != null)
				view.Window.InvalidateCursorRectsForView(view);
		}

		public static PointF GetEventLocation(this NSView view, NSEvent e)
		{
			return view.ConvertPointFromView(e.LocationInWindow, null);
		}

		public static RectangleF FocusedItemMarkFrame
		{
			get { return new RectangleF(0f, -3.5f, 3.5f, 7f); }
		}

		public static void DrawFocusedItemMark(LJD.Graphics g, float x, float y)
		{
			var sz = FocusedItemMarkFrame.Size;
			var focusedItemMarkPoints = new PointF[]
			{
				new PointF(x, y-sz.Height/2),
				new PointF(x+sz.Width, y),
				new PointF(x, y+sz.Height/2),
			};

			g.FillPolygon(blue, focusedItemMarkPoints);
		}

		static LJD.Brush blue = new LJD.Brush(Color.Blue);
	}
}

