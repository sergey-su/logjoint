using System;
using System.Linq;
using System.Collections.Generic;
using SD = System.Drawing;
using SD2D = System.Drawing.Drawing2D;

namespace LogJoint.Drawing
{
	partial class Graphics
	{
		internal System.Drawing.Graphics g;
		bool ownsGraphics;
		Stack<SD2D.GraphicsState> stateStack = new Stack<SD2D.GraphicsState>();


		public void Dispose()
		{
			if (ownsGraphics)
				g.Dispose();
		}

		partial void Init(SD.Graphics g, bool ownsGraphics)
		{
			this.g = g;
			this.ownsGraphics = ownsGraphics;
		}

		partial void FillRectangleImp(Brush brush, RectangleF rect)
		{
			g.FillRectangle(brush.b, rect.ToSystemDrawingObject());
		}

		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format)
		{
			FixGdiPlusStringSize(ref s);
			if (format != null)
				g.DrawString(s, font.font, brush.b, pt.ToSystemDrawingObject(), format.format);
			else
				g.DrawString(s, font.font, brush.b, pt.ToSystemDrawingObject());
		}

		private static bool FixGdiPlusStringSize(ref string s)
		{
			if (s.Length < 32000)
				return false;
			s = s.Substring(0, 32000);
			return true;
		}

		partial void DrawStringImp(string s, Font font, Brush brush, RectangleF frame, StringFormat format)
		{
			if (format != null)
				g.DrawString(s, font.font, brush.b, frame.ToSystemDrawingObject(), format.format);
			else
				g.DrawString(s, font.font, brush.b, frame.ToSystemDrawingObject());
		}

		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret)
		{
			if (FixGdiPlusStringSize(ref str))
			{
				if (range.First >= str.Length)
				{
					range.First = str.Length - 1;
					range.Length = 0;
				}
				else if (range.First + range.Length >= str.Length)
				{
					range.Length = str.Length - range.First;
				}
			}
			format.format.SetMeasurableCharacterRanges(new SD.CharacterRange[] { 
				new SD.CharacterRange(range.First, range.Length)
			});
			var regions = g.MeasureCharacterRanges(str, font.font, new SD.RectangleF(0, 0, 100500, 100000), format.format);
			var bounds = regions[0].GetBounds(g);
			regions[0].Dispose();
			ret = bounds.ToRectangleF();
		}

		partial void DrawRectangleImp (Pen pen, RectangleF rect)
		{
			g.DrawRectangle(pen.pen, rect.X, rect.Y, rect.Width, rect.Height);
		}

		partial void DrawEllipseImp(Pen pen, RectangleF rect)
		{
			g.DrawEllipse(pen.pen, rect.ToSystemDrawingObject());
		}

		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2)
		{
			g.DrawLine(pen.pen, pt1.ToSystemDrawingObject(), pt2.ToSystemDrawingObject());
		}

		partial void MeasureStringImp(string text, Font font, ref SizeF ret)
		{
			ret = g.MeasureString(text, font.font).ToSizeF();
		}

		partial void MeasureStringImp(string text, Font font, StringFormat format, SizeF frameSz, ref SizeF ret)
		{
			ret = g.MeasureString(text, font.font, frameSz.ToSystemDrawingObject(), format.format).ToSizeF();
		}

		partial void DrawImageImp(Image image, RectangleF bounds)
		{
			try
			{
				g.InterpolationMode = SD2D.InterpolationMode.HighQualityBicubic;
				g.DrawImage(image.image, bounds.ToSystemDrawingObject());
			}
			catch (OutOfMemoryException)
			{
				// under low memory DrawImage throws OOM even if everything else works fine.
				// regress the image but do not crash the app.
				g.DrawRectangle(System.Drawing.Pens.Red, bounds.X, bounds.Y, bounds.Width, bounds.Height);
			}
		}

		partial void DrawLinesImp(Pen pen, PointF[] points)
		{
			g.DrawLines(pen.pen, points.Select(p => p.ToSystemDrawingObject()).ToArray());
		}

		partial void FillPolygonImp(Brush brush, PointF[] points)
		{
			g.FillPolygon(brush.b, points.Select(p => p.ToSystemDrawingObject()).ToArray());
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
			g.SmoothingMode = value ? SD2D.SmoothingMode.AntiAlias : SD2D.SmoothingMode.None;
		}

		partial void EnableTextAntialiasingImp(bool value)
		{
			g.TextRenderingHint = value ? SD.Text.TextRenderingHint.AntiAlias : SD.Text.TextRenderingHint.SystemDefault;
		}

		partial void IntersectClipImp(RectangleF r)
		{
			g.IntersectClip(r.ToSystemDrawingObject());
		}

		partial void TranslateTransformImp(float x, float y)
		{
			g.TranslateTransform(x, y);
		}

		partial void ScaleTransformImp(float x, float y)
		{
			g.ScaleTransform(x, y);
		}

		partial void RotateTransformImp(float degrees)
		{
			g.RotateTransform(degrees);
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

		public static SD2D.GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
		{
			var path = new SD2D.GraphicsPath();
			roundRadius = Math.Min(roundRadius, Math.Min(rectangle.Width / 2, rectangle.Height / 2));
			if (roundRadius <= 1)
			{
				path.AddRectangle(rectangle.ToSystemDrawingObject());
				return path;
			}
			RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
			path.StartFigure();
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Bottom - 1, roundRadius), 0, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Bottom - 1, roundRadius), 90, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Top, roundRadius), 180, 90);
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Top, roundRadius), 270, 90);
			path.CloseFigure();
			return path;
		}

		private static SD.RectangleF RoundBounds(float x, float y, float rounding)
		{
			return new SD.RectangleF(x - rounding, y - rounding, 2 * rounding, 2 * rounding);
		}
	};
}