using System.Drawing;
using CoreGraphics;
using AppKit;
using Foundation;
using CoreText;
using LogJoint.Drawing;
using System;

namespace LogJoint.Drawing
{
	partial class Graphics
	{
		internal CGContext context;
		internal Profiling.Counters.Writer perfCountersWriter = Profiling.Counters.Writer.Null;
		internal PerformanceCounters perfCounters = PerformanceCounters.Null;
		internal LRUCache<(string, Font, Brush, StringFormat), NSAttributedString> drawStringPointCache =
			new LRUCache<(string, Font, Brush, StringFormat), NSAttributedString>(128, str => str.Dispose());

		public void Dispose()
		{
			drawStringPointCache.Clear();
		}

		partial void InitFromContext(CGContext context)
		{
			this.context = context;
		}

		partial class PerformanceCounters
		{
			partial void Init (Profiling.Counters countersContainer)
			{
				if (countersContainer == null)
					return;
				drawStringPoint = countersContainer.AddCounter ("DrawString(pt)", unit: "ms", reportCount: true);
				drawStringPoint_CreateAS = countersContainer.AddCounter ("DrawString(pt).CreateAttributedString", unit: "ms");
				drawStringPoint_Draw1 = countersContainer.AddCounter ("DrawString(pt).Draw(1)", unit: "ms");
				drawStringPoint_Draw2 = countersContainer.AddCounter ("DrawString(pt).Draw(2)", unit: "ms");
				fillRectangle = countersContainer.AddCounter ("FillRectangle", unit: "ms", reportCount: true);
				fillRoundRectangle = countersContainer.AddCounter ("FillRoundRectangle", unit: "ms");
				measureCharRange = countersContainer.AddCounter ("MeasureCharRange", unit: "ms");
			}
		}

		partial void ConfigureProfilingImpl (PerformanceCounters counters, Profiling.Counters.Writer writer)
		{
			this.perfCounters = counters;
			this.perfCountersWriter = writer;
		}

		partial void FillRectangleImp(Brush brush, RectangleF rect)
		{
			using (perfCountersWriter.IncrementTicks (perfCounters.fillRectangle)) {
				SetFill (brush);
				context.FillRect (rect.ToCGRect ());
			}
		}

		partial void FillRoundRectangleImp(Brush brush, RectangleF rect, float radius)
		{
			using (perfCountersWriter.IncrementTicks (perfCounters.fillRoundRectangle)) {
				AddClosedRoundRectanglePath (rect, radius);
				FillPath (brush);
			}
		}


		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format)
		{
			using (perfCountersWriter.IncrementTicks (perfCounters.drawStringPoint)) {

				NSAttributedString attributedString;

				using (perfCountersWriter.IncrementTicks (perfCounters.drawStringPoint_CreateAS)) {
					var cacheKey = (s, font, brush, format);
					if (!drawStringPointCache.TryGetValue(cacheKey, out attributedString))
						drawStringPointCache.Set(cacheKey, attributedString = CreateAttributedString (s, font, format, brush));
				}

				if (format != null && (format.horizontalAlignment != StringAlignment.Near || format.verticalAlignment != StringAlignment.Near)) {
					var sz = attributedString.Size;
					if (format.horizontalAlignment == StringAlignment.Center) {
						pt.X -= (float)sz.Width / 2;
					} else if (format.horizontalAlignment == StringAlignment.Far) {
						pt.X -= (float)sz.Width;
					}
					if (format.verticalAlignment == StringAlignment.Center) {
						pt.Y -= (float)sz.Height / 2;
					} else if (format.verticalAlignment == StringAlignment.Far) {
						pt.Y -= (float)sz.Height;
					}
					using (perfCountersWriter.IncrementTicks (perfCounters.drawStringPoint_Draw1))
						attributedString.DrawString (new RectangleF (pt, sz.ToSizeF ()).ToCGRect ());
				} else {
					using (perfCountersWriter.IncrementTicks (perfCounters.drawStringPoint_Draw2))
						attributedString.DrawString (pt.ToCGPoint ());
				}
			}
		}

		private NSMutableAttributedString CreateAttributedString(string text, Font font, StringFormat format, Brush brush) 
		{
			var range = new NSRange(0, text.Length);
			var stringAttrs = new NSMutableAttributedString (text);
			stringAttrs.BeginEditing();

			stringAttrs.AddAttribute (NSStringAttributeKey.Font, font.font, range);

			if (brush != null)
			{
				var brushColor = brush.color;
				var foregroundColor = NSColor.FromDeviceRgba(brushColor.R / 255f, brushColor.G / 255f, brushColor.B / 255f, brushColor.A / 255f);
				stringAttrs.AddAttribute(NSStringAttributeKey.ForegroundColor, foregroundColor, range);
			}

			if (format != null)
			{
				var para = new NSMutableParagraphStyle();
				if (format.horizontalAlignment == StringAlignment.Near)
					para.Alignment = NSTextAlignment.Left;
				else if (format.horizontalAlignment == StringAlignment.Center)
					para.Alignment = NSTextAlignment.Center;
				else if (format.horizontalAlignment == StringAlignment.Far)
					para.Alignment = NSTextAlignment.Right;

				if (format.lineBreakMode == LineBreakMode.WrapWords)
					para.LineBreakMode = NSLineBreakMode.ByWordWrapping;
				else if (format.lineBreakMode == LineBreakMode.WrapChars)
					para.LineBreakMode = NSLineBreakMode.CharWrapping;
				else if (format.lineBreakMode == LineBreakMode.SingleLineEndEllipsis)
				{
					para.LineBreakMode = NSLineBreakMode.TruncatingTail;
					para.TighteningFactorForTruncation = 0;
				}

				stringAttrs.AddAttribute(NSStringAttributeKey.ParagraphStyle, para, range);
			}

			if ((font.style & FontStyle.Underline) != 0)
			{
				stringAttrs.AddAttribute(NSStringAttributeKey.UnderlineStyle, new NSNumber(1), range);
			}
				
			stringAttrs.EndEditing();
			return stringAttrs;
		}

		partial void MeasureStringImp(string text, Font font, ref SizeF ret)
		{
			var attributedString = CreateAttributedString(text, font, null, null);
			ret = attributedString.Size.ToSizeF ();
		}

		partial void MeasureStringImp(string text, Font font, StringFormat format, SizeF frameSz, ref SizeF ret)
		{
			var attributedString = CreateAttributedString(text, font, format, null);
			using (var framesetter = new CTFramesetter(attributedString))
			{
				NSRange fitRange;
				ret = framesetter.SuggestFrameSize(new NSRange(0, 0), null, frameSz.ToCGSize(), out fitRange).ToSizeF ();
			}
		}

		partial void DrawStringImp(string s, Font font, Brush brush, RectangleF frame, StringFormat format)
		{
			var attributedString = CreateAttributedString(s, font, format, brush);
			if (format != null && (format.horizontalAlignment != StringAlignment.Near || format.verticalAlignment != StringAlignment.Near))
			{
				using (var framesetter = new CTFramesetter(attributedString))
				{
					NSRange fitRange;
					var sz = framesetter.SuggestFrameSize(new NSRange(0, 0), null, frame.Size.ToCGSize (), out fitRange);

					RectangleF newFrame = new RectangleF(new PointF(), sz.ToSizeF ());

					if (format.horizontalAlignment == StringAlignment.Near)
						newFrame.X = frame.X;
					else if (format.horizontalAlignment == StringAlignment.Center)
						newFrame.X = (float)(frame.Left + frame.Right - sz.Width) / 2;
					else if (format.horizontalAlignment == StringAlignment.Far)
						newFrame.X = (float)(frame.Right - sz.Width);
					
					if (format.verticalAlignment == StringAlignment.Near)
						newFrame.Y = frame.Y;
					else if (format.verticalAlignment == StringAlignment.Center)
					{
						newFrame.Inflate(0, 1);
						newFrame.Y = (float)(frame.Top + frame.Bottom - sz.Height) / 2;
					}
					else if (format.verticalAlignment == StringAlignment.Far)
						newFrame.Y = (float)(frame.Bottom - sz.Height);
					
					attributedString.DrawString(newFrame.ToCGRect ());
				}
			}
			else
			{
				attributedString.DrawString(frame.ToCGRect ());
			}
		}

		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret)
		{
			using (perfCountersWriter.IncrementTicks (perfCounters.measureCharRange)) {
				var attributedString = CreateAttributedString (str, font, format, null);
				CTLine line = new CTLine (attributedString);
				CTRun [] runArray = line.GetGlyphRuns ();
				if (runArray.Length > 0) {
					context.TextPosition = new CGPoint ();
					ret = runArray [0].GetImageBounds (context, new NSRange (range.First, range.Length)).ToRectangle ();
				}
			}
		}

		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2)
		{
			context.MoveTo (pt1.X, pt1.Y);
			context.AddLineToPoint (pt2.X, pt2.Y);
			StrokePath (pen, new Vector() { A = pt1, B = pt2 });
		}

		partial void DrawRectangleImp (Pen pen, RectangleF rect)
		{
			AddClosedRectanglePath(rect.Left, rect.Top, rect.Right, rect.Bottom);
			StrokePath(pen, null);
		}

		partial void DrawEllipseImp(Pen pen, RectangleF rect)
		{
			AddEllipsePath(rect.Left, rect.Top, rect.Right, rect.Bottom);
			StrokePath(pen, null);
		}

		partial void DrawRoundRectangleImp(Pen pen, RectangleF rect, float radius)
		{
			AddClosedRoundRectanglePath(rect, radius);
			StrokePath(pen, null);
		}

		partial void DrawImageImp(Image image, RectangleF bounds)
		{
			context.SaveState();
			context.TranslateCTM(bounds.Left, bounds.Bottom);
			context.ScaleCTM(1, -1);
			context.DrawImage(new CGRect(0, 0, bounds.Width, bounds.Height), image.image);
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

		void AddEllipsePath (float x1, float y1, float x2, float y2)
		{
			context.AddEllipseInRect(new CGRect(x1, y1, x2 - x1, y2 - y1));
		}

		void AddClosedRoundRectanglePath (RectangleF rect, float radius)
		{
			radius = Math.Min (radius, Math.Min (rect.Width/2, rect.Height/2));
			if (radius <= 1) {
				context.AddRect (rect.ToCGRect ());
				return;
			}
			float minx = rect.Left;
			float midx = (rect.Left + rect.Right) / 2f;
			float maxx = rect.Right; 

			float miny = rect.Top;
			float midy = (rect.Top + rect.Bottom) / 2f;
			float maxy = rect.Bottom;

			context.MoveTo(minx, midy); 
			context.AddArcToPoint(minx, miny, midx, miny, radius); 
			context.AddArcToPoint(maxx, miny, maxx, midy, radius); 
			context.AddArcToPoint(maxx, maxy, midx, maxy, radius); 
			context.AddArcToPoint(minx, maxy, minx, midy, radius); 
			context.ClosePath ();
		}

		partial void DrawLinesImp(Pen pen, PointF[] points)
		{
			if (points.Length < 2)
				return;
			PointF pt = points[0];
			context.MoveTo (pt.X, pt.Y);
			for (int p = 1; p < points.Length; ++p)
			{
				pt = points[p];
				context.AddLineToPoint (pt.X, pt.Y);
			}
			StrokePath(pen, new Vector() { A = points[points.Length - 2], B = points[points.Length - 1]});
		}

		void FillPath(Brush brush)
		{
			SetFill (brush);
			context.FillPath ();
		}

		void SetFill (Brush brush)
		{
			var c = brush.color;
			context.SetFillColor (c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
		}

		struct Vector
		{
			public PointF A, B;
		};

		void StrokePath(Pen pen, Vector? endVector)
		{
			var c = pen.color;
			context.SetStrokeColor(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
			context.SetLineWidth (pen.width == 0 ? 1 : pen.width);
			if (pen.dashPattern != null)
				context.SetLineDash(0, pen.dashPattern);
			else
				context.SetLineDash(0, null);
			context.DrawPath(CGPathDrawingMode.Stroke);
		}

		partial void PushStateImp()
		{
			context.SaveState();
		}

		partial void PopStateImp()
		{
			context.RestoreState();
		}

		partial void EnableAntialiasingImp(bool value)
		{
			context.SetAllowsAntialiasing(value);
		}

		partial void EnableTextAntialiasingImp(bool value)
		{
			context.SetShouldSmoothFonts(value);
		}

		partial void IntersectClipImp(RectangleF r)
		{
			context.ClipToRect(r.ToCGRect ());
		}

		partial void TranslateTransformImp(float x, float y)
		{
			context.TranslateCTM(x, y);
		}

		partial void ScaleTransformImp(float x, float y)
		{
			context.ScaleCTM(x, y);
		}

		partial void RotateTransformImp(float degrees)
		{
			context.RotateCTM((nfloat)(degrees * Math.PI/180));
		}

		partial void FillPolygonImp(Brush brush, PointF[] points)
		{
			if (points.Length < 2)
				return;
			PointF pt = points[0];
			context.MoveTo (pt.X, pt.Y);
			for (int p = 1; p < points.Length; ++p)
			{
				pt = points[p];
				context.AddLineToPoint (pt.X, pt.Y);
			}
			FillPath(brush);
		}
	};
}