using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;

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

		public static void AddRoundRect(GraphicsPath gp, Rectangle rect, int radius)
		{
			int diameter = radius * 2;
			Size size = new Size(diameter, diameter);
			Rectangle arc = new Rectangle(rect.Location, size);

			gp.AddArc(arc, 180, 90);

			arc.X = rect.Right - diameter;
			gp.AddArc(arc, 270, 90);

			arc.Y = rect.Bottom - diameter;
			gp.AddArc(arc, 0, 90);

			arc.X = rect.Left;
			gp.AddArc(arc, 90, 90);

			gp.CloseFigure();
		}

		public class DrawShadowRect : IDisposable
		{
			readonly Color color;
			SolidBrush inner, border1, border2, edge1, edge2, edge3;

			SolidBrush CreateHalftone(int alpha)
			{
				return new SolidBrush(Color.FromArgb(alpha, color));
			}

			/// <summary>
			/// The minimum size of a rectangle that can be rendered by Draw()
			/// </summary>
			public static readonly Size MinimumRectSize = new Size(4, 4);

			public DrawShadowRect(Color cl)
			{
				color = cl;
				inner = CreateHalftone(255);
				border1 = CreateHalftone(191);
				border2 = CreateHalftone(63);
				edge1 = CreateHalftone(143);
				edge2 = CreateHalftone(47);
				edge3 = CreateHalftone(15);
			}
			public void Dispose()
			{
				inner.Dispose();
				border1.Dispose();
				border2.Dispose();
				edge1.Dispose();
				edge2.Dispose();
				edge3.Dispose();
			}

			public static bool IsValidRectToDrawShadow(Rectangle r)
			{
				return r.Width >= MinimumRectSize.Width && r.Height >= MinimumRectSize.Height;
			}

			public void Draw(Graphics g, Rectangle r, Border3DSide sides)
			{
				if (!IsValidRectToDrawShadow(r))
				{
					throw new ArgumentException("Rect is too small", "r");
				}

				r.Inflate(-2, -2);

				if ((sides & Border3DSide.Middle) != 0)
				{
					g.FillRectangle(inner, r);
				}

				if ((sides & Border3DSide.Top) != 0)
				{
					g.FillRectangle(border1, r.Left, r.Top - 1, r.Width, 1);
					g.FillRectangle(border2, r.Left, r.Top - 2, r.Width, 1);
				}
				if ((sides & Border3DSide.Right) != 0)
				{
					g.FillRectangle(border1, r.Right, r.Top, 1, r.Height);
					g.FillRectangle(border2, r.Right + 1, r.Top, 1, r.Height);
				}
				if ((sides & Border3DSide.Bottom) != 0)
				{
					g.FillRectangle(border1, r.Left, r.Bottom, r.Width, 1);
					g.FillRectangle(border2, r.Left, r.Bottom + 1, r.Width, 1);
				}
				if ((sides & Border3DSide.Left) != 0)
				{
					g.FillRectangle(border1, r.Left - 1, r.Top, 1, r.Height);
					g.FillRectangle(border2, r.Left - 2, r.Top, 1, r.Height);
				}

				if ((sides & Border3DSide.Left) != 0 && (sides & Border3DSide.Top) != 0)
				{
					g.FillRectangle(edge1, r.Left - 1, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 2, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 1, r.Top - 2, 1, 1);
					g.FillRectangle(edge3, r.Left - 2, r.Top - 2, 1, 1);
				}

				if ((sides & Border3DSide.Top) != 0 && (sides & Border3DSide.Right) != 0)
				{
					g.FillRectangle(edge1, r.Right, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Right, r.Top - 2, 1, 1);
					g.FillRectangle(edge2, r.Right + 1, r.Top - 1, 1, 1);
					g.FillRectangle(edge3, r.Right + 1, r.Top - 2, 1, 1);
				}

				if ((sides & Border3DSide.Right) != 0 && (sides & Border3DSide.Bottom) != 0)
				{
					g.FillRectangle(edge1, r.Right, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Right + 1, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Right, r.Bottom + 1, 1, 1);
					g.FillRectangle(edge3, r.Right + 1, r.Bottom + 1, 1, 1);
				}

				if ((sides & Border3DSide.Bottom) != 0 && (sides & Border3DSide.Left) != 0)
				{
					g.FillRectangle(edge1, r.Left - 1, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Left - 1, r.Bottom + 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 2, r.Bottom, 1, 1);
					g.FillRectangle(edge3, r.Left - 2, r.Bottom + 1, 1, 1);
				}
			}
		};
	}
}
