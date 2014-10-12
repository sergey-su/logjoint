using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace LogJoint.UI
{
	static class UIUtils
	{
		static GraphicsPath focusedItemMark;
		public static Rectangle FocusedItemMarkBounds
		{
			get { return new Rectangle(0, -3, 3, 6); }
		}
		public static void DrawFocusedItemMark(Graphics g, int x, int y)
		{
			if (focusedItemMark == null)
			{
				focusedItemMark = new GraphicsPath();
				focusedItemMark.AddPolygon(new Point[]{
					new Point(0, -3),
					new Point(2, 0),
					new Point(0, 3),
				});
			}

			GraphicsState state = g.Save();
			g.TranslateTransform(x, y);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.FillPath(Brushes.Blue, focusedItemMark);
			g.Restore(state);
		}

		public static void DrawDragEllipsis(Graphics g, Rectangle r)
		{
			int y = r.Top + 1;
			for (int i = r.Left; i < r.Right; i += 5)
			{
				g.FillRectangle(Brushes.White, i + 1, y + 1, 2, 2);
				g.FillRectangle(Brushes.DarkGray, i, y, 2, 2);
			}
		}
	}
}
