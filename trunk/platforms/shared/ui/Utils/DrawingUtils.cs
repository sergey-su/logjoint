using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;
using LogJoint.Drawing;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace LogJoint.Drawing
{
	public static class DrawingUtils
	{
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

	[Flags]
	public enum ShadowSide
	{
		Left = 1,
		Top = 2,
		Right = 4,
		Bottom = 8,
		Middle = 2048,
		All = 2063,
	}

	public class DrawShadowRect : IDisposable
	{
		readonly Color color;
		Brush inner, border1, border2, edge1, edge2, edge3;

		Brush CreateHalftone(int alpha)
		{
			return new Brush(Color.FromArgb(alpha, color));
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

		public void Draw(Graphics g, Rectangle r, ShadowSide sides)
		{
			if (!IsValidRectToDrawShadow(r))
			{
				throw new ArgumentException("Rect is too small", "r");
			}

			r.Inflate(-2, -2);

			if ((sides & ShadowSide.Middle) != 0)
			{
				g.FillRectangle(inner, r);
			}

			if ((sides & ShadowSide.Top) != 0)
			{
				g.FillRectangle(border1, r.Left, r.Top - 1, r.Width, 1);
				g.FillRectangle(border2, r.Left, r.Top - 2, r.Width, 1);
			}
			if ((sides & ShadowSide.Right) != 0)
			{
				g.FillRectangle(border1, r.Right, r.Top, 1, r.Height);
				g.FillRectangle(border2, r.Right + 1, r.Top, 1, r.Height);
			}
			if ((sides & ShadowSide.Bottom) != 0)
			{
				g.FillRectangle(border1, r.Left, r.Bottom, r.Width, 1);
				g.FillRectangle(border2, r.Left, r.Bottom + 1, r.Width, 1);
			}
			if ((sides & ShadowSide.Left) != 0)
			{
				g.FillRectangle(border1, r.Left - 1, r.Top, 1, r.Height);
				g.FillRectangle(border2, r.Left - 2, r.Top, 1, r.Height);
			}

			if ((sides & ShadowSide.Left) != 0 && (sides & ShadowSide.Top) != 0)
			{
				g.FillRectangle(edge1, r.Left - 1, r.Top - 1, 1, 1);
				g.FillRectangle(edge2, r.Left - 2, r.Top - 1, 1, 1);
				g.FillRectangle(edge2, r.Left - 1, r.Top - 2, 1, 1);
				g.FillRectangle(edge3, r.Left - 2, r.Top - 2, 1, 1);
			}

			if ((sides & ShadowSide.Top) != 0 && (sides & ShadowSide.Right) != 0)
			{
				g.FillRectangle(edge1, r.Right, r.Top - 1, 1, 1);
				g.FillRectangle(edge2, r.Right, r.Top - 2, 1, 1);
				g.FillRectangle(edge2, r.Right + 1, r.Top - 1, 1, 1);
				g.FillRectangle(edge3, r.Right + 1, r.Top - 2, 1, 1);
			}

			if ((sides & ShadowSide.Right) != 0 && (sides & ShadowSide.Bottom) != 0)
			{
				g.FillRectangle(edge1, r.Right, r.Bottom, 1, 1);
				g.FillRectangle(edge2, r.Right + 1, r.Bottom, 1, 1);
				g.FillRectangle(edge2, r.Right, r.Bottom + 1, 1, 1);
				g.FillRectangle(edge3, r.Right + 1, r.Bottom + 1, 1, 1);
			}

			if ((sides & ShadowSide.Bottom) != 0 && (sides & ShadowSide.Left) != 0)
			{
				g.FillRectangle(edge1, r.Left - 1, r.Bottom, 1, 1);
				g.FillRectangle(edge2, r.Left - 1, r.Bottom + 1, 1, 1);
				g.FillRectangle(edge2, r.Left - 2, r.Bottom, 1, 1);
				g.FillRectangle(edge3, r.Left - 2, r.Bottom + 1, 1, 1);
			}
		}
	};
}
