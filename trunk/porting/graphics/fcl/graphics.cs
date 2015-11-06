using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LogJoint.Drawing
{
	partial class Graphics
	{
		internal System.Drawing.Graphics g;
		bool ownsGraphics;
		Stack<GraphicsState> stateStack = new Stack<GraphicsState>();

		public void Dispose()
		{
			if (ownsGraphics)
				g.Dispose();
		}

		partial void Init(System.Drawing.Graphics g, bool ownsGraphics)
		{
			this.g = g;
			this.ownsGraphics = ownsGraphics;
		}

		partial void FillRectangleImp(Brush brush, RectangleF rect)
		{
			g.FillRectangle(brush.b, rect);
		}

		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format)
		{
			if (format != null)
				g.DrawString(s, font.font, brush.b, pt, format.format);
			else
				g.DrawString(s, font.font, brush.b, pt);
		}

		partial void DrawStringImp(string s, Font font, Brush brush, RectangleF frame, StringFormat format)
		{
			if (format != null)
				g.DrawString(s, font.font, brush.b, frame, format.format);
			else
				g.DrawString(s, font.font, brush.b, frame);
		}

		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret)
		{
			format.format.SetMeasurableCharacterRanges(new CharacterRange[] { 
				range 
			});
			var regions = g.MeasureCharacterRanges(str, font.font, new RectangleF(0, 0, 100500, 100000), format.format);
			var bounds = regions[0].GetBounds(g);
			regions[0].Dispose();
			ret = bounds;
		}

		partial void DrawRectangleImp (Pen pen, RectangleF rect)
		{
			g.DrawRectangle(pen.pen, rect.ToRectangle());
		}

		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2)
		{
			g.DrawLine(pen.pen, pt1, pt2);
		}

		partial void MeasureStringImp(string text, Font font, ref SizeF ret)
		{
			ret = g.MeasureString(text, font.font);
		}

		partial void MeasureStringImp(string text, Font font, StringFormat format, SizeF frameSz, ref SizeF ret)
		{
			ret = g.MeasureString(text, font.font, frameSz, format.format);
		}

		partial void DrawImageImp(Image image, RectangleF bounds)
		{
			g.DrawImage(image.image, bounds);
		}

		partial void DrawLinesImp(Pen pen, PointF[] points)
		{
			g.DrawLines(pen.pen, points);
		}

		partial void FillPolygonImp(Brush brush, PointF[] points)
		{
			g.FillPolygon(brush.b, points);
		}

		partial void PushStateImp()
		{
			stateStack.Push(g.Save());
		}

		partial void PopStateImp()
		{
			g.Restore(stateStack.Peek());
			stateStack.Pop();
		}

		partial void EnableAntialiasingImp(bool value)
		{
			g.SmoothingMode = value ? SmoothingMode.AntiAlias : SmoothingMode.None;
		}

		partial void IntsersectClipImp(RectangleF r)
		{
			g.IntersectClip(r);
		}

		partial void DrawRoundRectangleImp(Pen pen, RectangleF rect, float radius)
		{
			using (var gp = RoundRect(rect, radius))
				g.DrawPath(pen.pen, gp);
		}

		partial void FillRoundRectangleImp(Brush brush, RectangleF rect, float radius)
		{
			using (var gp = RoundRect(rect, radius))
				g.FillPath(brush.b, gp);
		}

		public static GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
		{
			RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
			var path = new GraphicsPath();
			path.StartFigure();
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Bottom - 1, roundRadius), 0, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Bottom - 1, roundRadius), 90, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Top, roundRadius), 180, 90);
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Top, roundRadius), 270, 90);
			path.CloseFigure();
			return path;
		}

		private static RectangleF RoundBounds(float x, float y, float rounding)
		{
			return new RectangleF(x - rounding, y - rounding, 2 * rounding, 2 * rounding);
		}
	};
}