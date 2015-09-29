using System.Drawing;

namespace LogJoint.Drawing
{
	partial class Graphics
	{
		internal System.Drawing.Graphics g;

		public void Dispose()
		{
			// todo: dispose of not
		}

		partial void Init(System.Drawing.Graphics g)
		{
			this.g = g;
		}

		partial void FillRectangleImp(Brush brush, Rectangle rect)
		{
			g.FillRectangle(brush.b, rect);
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

		partial void DrawImageImp(Image image, RectangleF bounds)
		{
			g.DrawImage(image.image, bounds);
		}
	};
}