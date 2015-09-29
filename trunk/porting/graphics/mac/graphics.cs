using System.Drawing;
using MonoMac.CoreGraphics;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.CoreText;

namespace LogJoint.Drawing
{
	partial class Graphics
	{
		internal CGContext context;

		public void Dispose()
		{
		}

		partial void InitFromCurrentContext()
		{
			var cc = NSGraphicsContext.CurrentContext;
			context =  cc != null ? cc.GraphicsPort : null;
		}

		partial void FillRectangleImp(Brush brush, Rectangle rect)
		{
			AddClosedRectanglePath(rect.Left, rect.Top, rect.Right, rect.Bottom);
			FillPath(brush);
		}

		partial void FillRectangleImp(Brush brush, RectangleF rect)
		{
			AddClosedRectanglePath(rect.Left, rect.Top, rect.Right, rect.Bottom);
			FillPath(brush);
		}

		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format)
		{
			var attributedString = CreateAttributedString(s, font, format, brush);
			attributedString.DrawString(pt);
		}

		private NSMutableAttributedString CreateAttributedString(string text, Font font, StringFormat format, Brush brush) 
		{
			var stringAttrs = new CTStringAttributes ();
		
			stringAttrs.Font = font.font;

			if (brush != null)
			{
				var brushColor = brush.color;
				var foregroundColor = new CGColor(brushColor.R / 255f, brushColor.G / 255f, brushColor.B / 255f, brushColor.A / 255f);
				stringAttrs.ForegroundColor = foregroundColor;
				stringAttrs.ForegroundColorFromContext = false;
			}

			var paraStyle = new CTParagraphStyleSettings()
			{
				Alignment = CTTextAlignment.Left
			};

			//stringAttrs.ParagraphStyle = new CTParagraphStyle(paraStyle);

			return new NSMutableAttributedString(text, stringAttrs.Dictionary);
		}

		partial void MeasureStringImp(string text, Font font, ref SizeF ret)
		{
			var attributedString = CreateAttributedString(text, font, null, null);
			ret = attributedString.Size;
		}

		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret)
		{
			var attributedString = CreateAttributedString(str, font, format, null);
			CTLine line = new CTLine (attributedString);
			CTRun[] runArray = line.GetGlyphRuns ();
			if (runArray.Length > 0)
			{
				context.TextPosition = new PointF();
				ret = runArray[0].GetImageBounds(context, new NSRange(range.First, range.Length));
			}
		}

		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2)
		{
			context.MoveTo (pt1.X, pt1.Y);
			context.AddLineToPoint (pt2.X, pt2.Y);
			StrokePath (pen);
		}

		partial void DrawRectangleImp (Pen pen, RectangleF rect)
		{
			AddClosedRectanglePath(rect.Left, rect.Top, rect.Right, rect.Bottom);
			StrokePath(pen);
		}

		partial void DrawImageImp(Image image, RectangleF bounds)
		{
			context.SaveState();
			context.TranslateCTM(bounds.Left, bounds.Bottom);
			context.ScaleCTM(1, -1);
			context.DrawImage(new RectangleF(0, 0, bounds.Width, bounds.Height), image.image);
			context.RestoreState();
		}

		void AddClosedRectanglePath (float x1, float y1, float x2, float y2)
		{
			context.MoveTo (x1, y1);
			context.AddLineToPoint (x1, y2);
			context.AddLineToPoint (x2, y2);
			context.AddLineToPoint (x2, y1);
			context.ClosePath ();
		}

		void FillPath(Brush brush)
		{
			var c = brush.color;
			context.SetFillColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
			context.FillPath ();
		}

		void StrokePath(Pen pen)
		{
			var c = pen.color;
			context.SetStrokeColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
			context.SetLineWidth (pen.width == 0 ? 1 : pen.width);
			context.DrawPath(CGPathDrawingMode.Stroke);
		}
	};
}