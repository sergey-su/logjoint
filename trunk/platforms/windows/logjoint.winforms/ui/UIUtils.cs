using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace LogJoint.UI
{
	static class UIUtils
	{
		static GraphicsPath focusedItemMark;
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
	}
}
