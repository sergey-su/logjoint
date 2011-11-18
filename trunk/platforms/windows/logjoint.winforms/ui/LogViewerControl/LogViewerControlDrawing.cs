using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LogJoint.UI
{
	internal class DrawingVisitor : IMessageBaseVisitor
	{
		public DrawContext ctx;
		public DrawingUtils.Metrics m;
		public Func<MessageBase, Tuple<int, int>> inplaceHighlightHandler;

		public void FillBackground(MessageBase msg)
		{
			DrawContext dc = ctx;
			Rectangle r = m.MessageRect;
			Brush b = null;
			if (msg.IsSelected)
			{
				if (dc.ControlFocused)
					b = dc.SelectedBkBrush;
				else
					b = dc.SelectedFocuslessBkBrush;
			}
			else if (msg.IsHighlighted)
			{
				b = dc.HighlightBrush;
			}
			else if (msg.Thread != null)
			{
				if (msg.Thread.IsDisposed)
					b = dc.DefaultBackgroundBrush;
				else
					b = msg.Thread.ThreadBrush;
			}
			if (b == null)
			{
				b = dc.DefaultBackgroundBrush;
			}
			dc.Canvas.FillRectangle(b, r);
		}

		public void DrawSelection(MessageBase msg)
		{
			DrawContext dc = ctx;
			if (dc.MessageFocused)
			{
				ControlPaint.DrawFocusRectangle(dc.Canvas, new Rectangle(
					FixedMetrics.CollapseBoxesAreaSize, m.MessageRect.Y,
					dc.ClientRect.Width - FixedMetrics.CollapseBoxesAreaSize, dc.MessageHeight
				), Color.Black, Color.Black);
			}
		}

		public void DrawMultiline(MessageBase msg)
		{
			if (msg.IsMultiLine)
			{
				ctx.Canvas.DrawString("\u00bf", ctx.NewlineFont,
					msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.CommentsBrush,
					m.OffsetTextRect.Right - ctx.CharSize.Width, m.MessageRect.Y);
			}
		}

		public Brush GetSelectedTextBrush(MessageBase msg)
		{
			return ctx.ControlFocused ? ctx.SelectedTextBrush : ctx.SelectedFocuslessTextBrush;
		}

		public void DrawTime(MessageBase msg)
		{
			if (ctx.ShowTime)
			{
				ctx.Canvas.DrawString(MessageBase.FormatTime(msg.Time, ctx.ShowMilliseconds),
					ctx.Font,
					msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.InfoMessagesBrush,
					m.TimePos.X, m.TimePos.Y);
			}
		}

		public void Visit(Content msg)
		{
			FillBackground(msg);
			DrawTime(msg);

			Brush b = msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.InfoMessagesBrush;
			if (msg.IsMultiLine)
			{
				DrawStringWithInplaceHightlight(msg, msg.Text.Value, ctx.Font, b, m.OffsetTextRect,
					ctx.MultilineTextFormat);
			}
			else
			{
				DrawStringWithInplaceHightlight(msg, msg.Text.Value, ctx.Font, b, m.OffsetTextRect.Location,
					ctx.SinglelineTextFormat);
			}

			DrawMultiline(msg);
			DrawSelection(msg);
		}

		public void Visit(FrameBegin msg)
		{
			FillBackground(msg);
			DrawTime(msg);

			Rectangle r = m.OffsetTextRect;

			bool collapsed = msg.Collapsed;

			Brush txtBrush = msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.InfoMessagesBrush;
			Brush commentsBrush = msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.CommentsBrush;

			string mark = FrameBegin.GetCollapseMark(collapsed);
			ctx.Canvas.DrawString(
				mark,
				ctx.Font,
				txtBrush,
				r.X, r.Y);

			r.X += (int)(ctx.CharSize.Width * (mark.Length + 1));

			if (msg.IsMultiLine)
			{
				DrawStringWithInplaceHightlight(msg, msg.Text.Value, ctx.Font, commentsBrush, r,
					ctx.MultilineTextFormat);
			}
			else
			{
				DrawStringWithInplaceHightlight(msg, msg.Text.Value, ctx.Font, commentsBrush, r.Location,
					ctx.SinglelineTextFormat);
			}

			DrawSelection(msg);
		}

		public void Visit(FrameEnd msg)
		{
			FillBackground(msg);
			DrawTime(msg);

			RectangleF r = m.OffsetTextRect;

			ctx.Canvas.DrawString("}", ctx.Font, msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.InfoMessagesBrush, r.X, r.Y);
			if (msg.Start != null)
			{
				r.X += ctx.CharSize.Width * 2;
				Brush commentsBrush = msg.IsSelected ? GetSelectedTextBrush(msg) : ctx.CommentsBrush;
				ctx.Canvas.DrawString("//", ctx.Font, commentsBrush, r.X, r.Y);
				r.X += ctx.CharSize.Width * 3;
				if (msg.IsMultiLine)
				{
					DrawStringWithInplaceHightlight(msg, msg.Start.Name.Value, ctx.Font, commentsBrush, r,
						ctx.MultilineTextFormat);
				}
				else
				{
					DrawStringWithInplaceHightlight(msg, msg.Start.Name.Value, ctx.Font, commentsBrush, r.Location,
						ctx.SinglelineTextFormat);
				}

			}
			DrawSelection(msg);
		}

		void FillInplaceHightlightRectangle(RectangleF rect)
		{
			using (GraphicsPath path = DrawingUtils.RoundRect(
					RectangleF.Inflate(rect, 2, 0), 3))
			{
				ctx.Canvas.SmoothingMode = SmoothingMode.AntiAlias;
				ctx.Canvas.FillPath(ctx.InplaceHightlightBackground, path);
				ctx.Canvas.SmoothingMode = SmoothingMode.Default;
			}
		}

		void DrawStringWithInplaceHightlight(MessageBase msg, string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
		{
			if (inplaceHighlightHandler != null)
			{
				var hlRange = inplaceHighlightHandler(msg);
				if (hlRange != null)
				{
					FillInplaceHightlightRectangle(DrawingUtils.GetMessageTextSubstringBounds(
						ctx.Canvas, m.MessageRect, msg, hlRange.Item1, hlRange.Item2, font, layoutRectangle.X, format));
				}
			}

			ctx.Canvas.DrawString(s, font, brush, layoutRectangle, format);
		}

		void DrawStringWithInplaceHightlight(MessageBase msg, string s, Font font, Brush brush, PointF location, StringFormat format)
		{
			if (inplaceHighlightHandler != null)
			{
				var hlRange = inplaceHighlightHandler(msg);
				if (hlRange != null)
				{
					FillInplaceHightlightRectangle(DrawingUtils.GetMessageTextSubstringBounds(ctx.Canvas, m.MessageRect,
							msg, hlRange.Item1, hlRange.Item2, font, location.X, format));
				}
			}

			ctx.Canvas.DrawString(s, font, brush, location, format);
		}
	};

	internal class DrawOutlineVisitor : IMessageBaseVisitor
	{
		public DrawContext drawContext;
		public DrawingUtils.Metrics metrics;

		public void Visit(Content msg)
		{
			Image icon = null;
			Image icon2 = null;
			if (msg.Severity == Content.SeverityFlag.Error)
				icon = drawContext.ErrorIcon;
			else if (msg.Severity == Content.SeverityFlag.Warning)
				icon = drawContext.WarnIcon;
			if (msg.IsBookmarked)
				if (icon == null)
					icon = drawContext.BookmarkIcon;
				else
					icon2 = drawContext.SmallBookmarkIcon;
			if (icon == null)
				return;
			int w = FixedMetrics.CollapseBoxesAreaSize;
			drawContext.Canvas.DrawImage(icon,
				icon2 == null ? (w - icon.Width) / 2 : 1,
				metrics.MessageRect.Y + (drawContext.MessageHeight - icon.Height) / 2,
				icon.Width,
				icon.Height
			);
			if (icon2 != null)
				drawContext.Canvas.DrawImage(icon2,
					w - icon2.Width - 1,
					metrics.MessageRect.Y + (drawContext.MessageHeight - icon2.Height) / 2,
					icon2.Width,
					icon2.Height
				);
		}

		public void Visit(FrameBegin msg)
		{
			Pen murkupPen = drawContext.OutlineMarkupPen;
			drawContext.Canvas.DrawRectangle(murkupPen, metrics.OulineBox);
			Point p = metrics.OulineBoxCenter;
			drawContext.Canvas.DrawLine(murkupPen, p.X - FixedMetrics.OutlineCrossSize / 2, p.Y, p.X + FixedMetrics.OutlineCrossSize / 2, p.Y);
			bool collapsed = msg.Collapsed;
			if (collapsed)
				drawContext.Canvas.DrawLine(murkupPen, p.X, p.Y - FixedMetrics.OutlineCrossSize / 2, p.X, p.Y + FixedMetrics.OutlineCrossSize / 2);
			if (msg.IsBookmarked)
			{
				Image icon = drawContext.SmallBookmarkIcon;
				drawContext.Canvas.DrawImage(icon,
					FixedMetrics.CollapseBoxesAreaSize - icon.Width - 1,
					metrics.MessageRect.Y + (drawContext.MessageHeight - icon.Height) / 2,
					icon.Width,
					icon.Height
				);
			}
		}

		public void Visit(FrameEnd msg)
		{
			if (msg.IsBookmarked)
			{
				Image icon = drawContext.BookmarkIcon;
				drawContext.Canvas.DrawImage(icon,
					(FixedMetrics.CollapseBoxesAreaSize - icon.Width) / 2,
					metrics.MessageRect.Y + (drawContext.MessageHeight - icon.Height) / 2,
					icon.Width,
					icon.Height
				);
			}
		}
	};

	internal class DrawHighlightVisitor : IMessageBaseVisitor
	{
		public DrawContext ctx;
		public HighlightRange lh;
		public DrawingUtils.Metrics m;

		void DrawHighlight(MessageBase msg, float textXPos)
		{
			DrawContext dc = ctx;
			GraphicsState state = dc.Canvas.Save();
			try
			{
				dc.Canvas.SmoothingMode = SmoothingMode.HighQuality;

				RectangleF tmp = DrawingUtils.GetMessageTextSubstringBounds(
					ctx.Canvas, m.MessageRect, msg, lh.Begin, lh.End, dc.Font,
					m.OffsetTextRect.X + textXPos, ctx.SinglelineTextFormat);

				using (GraphicsPath path = DrawingUtils.RoundRect(
						RectangleF.Inflate(tmp, 3, 2), dc.CharSize.Width / 2))
				{
					dc.Canvas.DrawPath(dc.HighlightPen, path);
				}
			}
			finally
			{
				dc.Canvas.Restore(state);
			}
		}

		public void Visit(Content msg)
		{
			DrawHighlight(msg, 0);
		}

		public void Visit(FrameBegin msg)
		{
			DrawHighlight(msg,
				ctx.CharSize.Width * (FrameBegin.GetCollapseMark(msg.Collapsed).Length + 1));
		}

		public void Visit(FrameEnd msg)
		{
			DrawHighlight(msg, ctx.CharSize.Width * 5);
		}
	};

	public class DrawContext
	{
		public SizeF CharSize;
		public double CharWidthDblPrecision;
		public int MessageHeight;
		public int TimeAreaSize;
		public Brush InfoMessagesBrush;
		public Font Font;
		public Font NewlineFont;
		public Brush CommentsBrush;
		public Brush DefaultBackgroundBrush;
		public Pen OutlineMarkupPen, SelectedOutlineMarkupPen;
		public Brush SelectedBkBrush;
		public Brush SelectedFocuslessBkBrush;
		public Brush SelectedTextBrush;
		public Brush SelectedFocuslessTextBrush;
		public Brush HighlightBrush;
		public Image ErrorIcon, WarnIcon, BookmarkIcon, SmallBookmarkIcon;
		public Pen HighlightPen;
		public Pen TimeSeparatorLine;
		public StringFormat MultilineTextFormat;
		public StringFormat SinglelineTextFormat;
		public Brush InplaceHightlightBackground;

		public Graphics Canvas;
		public int MessageIdx;
		public bool ShowTime;
		public bool ShowMilliseconds;
		public bool MessageFocused;
		public bool ControlFocused;
		public Point ScrollPos;
		public Rectangle ClientRect;
		public Point GetTextOffset(int level)
		{
			int x = FixedMetrics.CollapseBoxesAreaSize + FixedMetrics.LevelOffset * level - ScrollPos.X;
			if (ShowTime)
				x += TimeAreaSize;
			int y = MessageIdx * MessageHeight - ScrollPos.Y;
			return new Point(x, y);
		}
	};

	internal static class FixedMetrics
	{
		public const int CollapseBoxesAreaSize = 25;
		public const int OutlineBoxSize = 10;
		public const int OutlineCrossSize = 7;
		public const int LevelOffset = 15;
	}

	static class DrawingUtils
	{
		public struct Metrics
		{
			public Rectangle MessageRect;
			public Point TimePos;
			public Rectangle OffsetTextRect;
			public Point OulineBoxCenter;
			public Rectangle OulineBox;
		};

		public static Metrics GetMetrics(MessageBase msg, DrawContext dc)
		{
			Point offset = dc.GetTextOffset(msg.Level);

			Metrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				dc.ClientRect.Width,
				dc.MessageHeight
			);

			m.TimePos = new Point(
				FixedMetrics.CollapseBoxesAreaSize - dc.ScrollPos.X,
				m.MessageRect.Y
			);

			int charCount = msg.GetDisplayTextLength();
			if (msg.IsMultiLine)
			{
				charCount++;
			}

			m.OffsetTextRect = new Rectangle(
				offset.X,
				m.MessageRect.Y,
				(int)((double)charCount * dc.CharWidthDblPrecision),
				m.MessageRect.Height
			);

			m.OulineBoxCenter = new Point(
				msg.IsBookmarked ?
					FixedMetrics.OutlineBoxSize / 2 + 1 :
					FixedMetrics.CollapseBoxesAreaSize / 2,
				m.MessageRect.Y + dc.MessageHeight / 2
			);
			m.OulineBox = new Rectangle(
				m.OulineBoxCenter.X - FixedMetrics.OutlineBoxSize / 2,
				m.OulineBoxCenter.Y - FixedMetrics.OutlineBoxSize / 2,
				FixedMetrics.OutlineBoxSize,
				FixedMetrics.OutlineBoxSize
			);

			return m;
		}

		public static RectangleF GetMessageTextSubstringBounds(Graphics g, RectangleF messageRect,
			MessageBase msg, int substringBegin, int substringEnd, Font font, float textDrawingXPosition, StringFormat format)
		{
			using (var path = new GraphicsPath())
			{
				StringSlice text = msg.Text;
				StringSlice substring = StringSlice.Empty;
				if (msg.IsMultiLine)
				{
					var displayTextLength = msg.GetDisplayTextLength();
					if (substringBegin >= displayTextLength)
					{
						substringBegin = msg.GetDisplayTextLength();
						substring = new StringSlice("0");
					}
					else if (substringEnd > displayTextLength)
					{
						substringEnd = displayTextLength;
						substring = text.SubString(substringBegin, substringEnd - substringBegin + 1); // +1 to include 'newline' char
					}
				}
				if (substring.IsEmpty)
				{
					substring = text.SubString(substringBegin, substringEnd - substringBegin);
				}

				StringBuilder stringToMeasure = new StringBuilder();
				stringToMeasure.Append(text.Take(substringBegin).Select(c => c == '\t' ? c : ' ').ToArray());
				stringToMeasure.Append(substring.Select(c => c == '\t' ? c : '0').ToArray());

				path.AddString(stringToMeasure.ToString(),
					font.FontFamily, (int)font.Style, font.SizeInPoints, new PointF(), format);
				float f = g.DpiY / 72;
				RectangleF bounds = path.GetBounds();
				return new RectangleF(textDrawingXPosition + bounds.X * f, messageRect.Top + 1,
					bounds.Width * f, messageRect.Height - 2);
			}
		}

		public static GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
		{
			RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
			GraphicsPath path = new GraphicsPath();
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
