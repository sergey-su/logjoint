using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.UI
{
	internal class DrawingVisitor : MessageTextHandlingVisitor, IMessageBaseVisitor
	{
		public int DisplayIndex;
		public int TextLineIdx;
		public Func<MessageBase, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler1;
		public Func<MessageBase, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler2;
		public Presenters.LogViewer.CursorPosition? CursorPosition;


		public override void Visit(Content msg)
		{
			base.Visit(msg);

			DrawTime(msg);

			DrawStringWithInplaceHightlight(msg, ctx.ShowRawMessages, TextLineIdx, ctx.Font, ctx.InfoMessagesBrush, m.OffsetTextRect.Location,
				ctx.TextFormat);

			DrawCursorIfNeeded(msg);

			FillOutlineBackground();
			DrawContentOutline(msg);
		}

		public override void Visit(FrameBegin msg)
		{
			base.Visit(msg);

			DrawTime(msg);

			Rectangle r = m.OffsetTextRect;

			bool collapsed = msg.Collapsed;

			Brush txtBrush = ctx.InfoMessagesBrush;
			Brush commentsBrush = ctx.CommentsBrush;

			string mark = FrameBegin.GetCollapseMark(collapsed);

			if (TextLineIdx == 0)
			{
				ctx.Canvas.DrawString(
					mark,
					ctx.Font,
					txtBrush,
					r.X, r.Y);
			}

			r.X += (int)(ctx.CharSize.Width * (mark.Length + 1));

			DrawStringWithInplaceHightlight(msg, ctx.ShowRawMessages, TextLineIdx, ctx.Font, commentsBrush, r.Location,
				ctx.TextFormat);

			DrawCursorIfNeeded(msg);

			FillOutlineBackground();
			DrawFrameBeginOutline(msg);
		}

		public override void Visit(FrameEnd msg)
		{
			base.Visit(msg);

			DrawTime(msg);

			RectangleF r = m.OffsetTextRect;

			if (TextLineIdx == 0)
			{
				ctx.Canvas.DrawString("}", ctx.Font, ctx.InfoMessagesBrush, r.X, r.Y);
			}
			if (msg.Start != null)
			{
				r.X += ctx.CharSize.Width * 2;
				Brush commentsBrush = ctx.CommentsBrush;
				ctx.Canvas.DrawString("//", ctx.Font, commentsBrush, r.X, r.Y);
				r.X += ctx.CharSize.Width * 3;
				DrawStringWithInplaceHightlight(msg, ctx.ShowRawMessages, TextLineIdx, ctx.Font, commentsBrush, r.Location,
					ctx.TextFormat);

			}

			DrawCursorIfNeeded(msg);

			FillOutlineBackground();
			DrawFrameEndOutline(msg);
		}

		protected override void HandleMessageText(MessageBase msg, float textXPos)
		{
			DrawMessageBackground(msg, textXPos);
		}



		void DrawTime(MessageBase msg)
		{
			if (ctx.ShowTime && TextLineIdx == 0)
			{
				ctx.Canvas.DrawString(msg.Time.ToUserFrendlyString(ctx.ShowMilliseconds),
					ctx.Font,
					ctx.InfoMessagesBrush,
					m.TimePos.X, m.TimePos.Y);
			}
		}

		void FillOutlineBackground()
		{
			ctx.Canvas.FillRectangle(ctx.DefaultBackgroundBrush, new Rectangle(0,
				m.MessageRect.Y, FixedMetrics.CollapseBoxesAreaSize, m.MessageRect.Height));
		}

		void DrawContentOutline(Content msg)
		{
			if (this.TextLineIdx != 0)
				return;
			Image icon = null;
			Image icon2 = null;
			if (msg.Severity == Content.SeverityFlag.Error)
				icon = ctx.ErrorIcon;
			else if (msg.Severity == Content.SeverityFlag.Warning)
				icon = ctx.WarnIcon;
			if (msg.IsBookmarked && TextLineIdx == 0)
				if (icon == null)
					icon = ctx.BookmarkIcon;
				else
					icon2 = ctx.SmallBookmarkIcon;
			if (icon == null)
				return;
			int w = FixedMetrics.CollapseBoxesAreaSize;
			ctx.Canvas.DrawImage(icon,
				icon2 == null ? (w - icon.Width) / 2 : 1,
				m.MessageRect.Y + (ctx.LineHeight - icon.Height) / 2,
				icon.Width,
				icon.Height
			);
			if (icon2 != null)
				ctx.Canvas.DrawImage(icon2,
					w - icon2.Width - 1,
					m.MessageRect.Y + (ctx.LineHeight - icon2.Height) / 2,
					icon2.Width,
					icon2.Height
				);
		}

		void DrawFrameBeginOutline(FrameBegin msg)
		{
			if (TextLineIdx == 0)
			{
				Pen murkupPen = ctx.OutlineMarkupPen;
				ctx.Canvas.DrawRectangle(murkupPen, m.OulineBox);
				Point p = m.OulineBoxCenter;
				ctx.Canvas.DrawLine(murkupPen, p.X - FixedMetrics.OutlineCrossSize / 2, p.Y, p.X + FixedMetrics.OutlineCrossSize / 2, p.Y);
				bool collapsed = msg.Collapsed;
				if (collapsed)
					ctx.Canvas.DrawLine(murkupPen, p.X, p.Y - FixedMetrics.OutlineCrossSize / 2, p.X, p.Y + FixedMetrics.OutlineCrossSize / 2);
				if (msg.IsBookmarked)
				{
					Image icon = ctx.SmallBookmarkIcon;
					ctx.Canvas.DrawImage(icon,
						FixedMetrics.CollapseBoxesAreaSize - icon.Width - 1,
						m.MessageRect.Y + (ctx.LineHeight - icon.Height) / 2,
						icon.Width,
						icon.Height
					);
				}
			}
		}

		void DrawFrameEndOutline(FrameEnd msg)
		{
			if (msg.IsBookmarked && TextLineIdx == 0)
			{
				Image icon = ctx.BookmarkIcon;
				ctx.Canvas.DrawImage(icon,
					(FixedMetrics.CollapseBoxesAreaSize - icon.Width) / 2,
					m.MessageRect.Y + (ctx.LineHeight - icon.Height) / 2,
					icon.Width,
					icon.Height
				);
			}
		}
		
		void DrawMessageBackground(MessageBase msg, float textXPos)
		{
			DrawContext dc = ctx;
			Rectangle r = m.MessageRect;
			r.Offset(FixedMetrics.CollapseBoxesAreaSize, 0);
			Brush b = null;

			if (msg.IsHighlighted)
			{
				b = dc.HighlightBrush;
			}
			else if (msg.Thread != null)
			{
				var coloring = dc.Coloring;
				if (coloring == ColoringMode.None)
					b = dc.DefaultBackgroundBrush;
				else if (msg.Thread.IsDisposed)
					b = dc.DefaultBackgroundBrush;
				else if (coloring == ColoringMode.Threads)
					b = msg.Thread.ThreadBrush;
				else if (coloring == ColoringMode.Sources)
					b = msg.LogSource.IsDisposed ? dc.DefaultBackgroundBrush : msg.LogSource.SourceBrush;
			}
			if (b == null)
			{
				b = dc.DefaultBackgroundBrush;
			}
			dc.Canvas.FillRectangle(b, r);

			var normalizedSelection = dc.NormalizedSelection;
			if (!normalizedSelection.IsEmpty
			 && DisplayIndex >= normalizedSelection.First.DisplayIndex
			 && DisplayIndex <= normalizedSelection.Last.DisplayIndex)
			{
				int selectionStartIdx;
				int selectionEndIdx;
				var line = dc.GetTextToDisplay(msg).GetNthTextLine(TextLineIdx);
				if (DisplayIndex == normalizedSelection.First.DisplayIndex)
					selectionStartIdx = normalizedSelection.First.LineCharIndex;
				else
					selectionStartIdx = 0;
				if (DisplayIndex == normalizedSelection.Last.DisplayIndex)
					selectionEndIdx = normalizedSelection.Last.LineCharIndex;
				else
					selectionEndIdx = line.Length;
				if (selectionStartIdx < selectionEndIdx)
				{
					RectangleF tmp = DrawingUtils.GetTextSubstringBounds(
						ctx.Canvas, m.MessageRect, line.Value,
						selectionStartIdx, selectionEndIdx, dc.Font,
						m.OffsetTextRect.X + textXPos, ctx.TextFormat);
					dc.Canvas.FillRectangle(dc.SelectedBkBrush, tmp);
				}
			}

			if (ctx.ShowTime)
			{
				float x = FixedMetrics.CollapseBoxesAreaSize + ctx.TimeAreaSize - ctx.ScrollPos.X - 2;
				if (x > FixedMetrics.CollapseBoxesAreaSize)
					ctx.Canvas.DrawLine(ctx.TimeSeparatorLine, x, m.MessageRect.Y, x, m.MessageRect.Bottom);
			}
		}

		void DrawCursorIfNeeded(MessageBase msg)
		{
			if (!CursorPosition.HasValue)
				return;
			msg.Visit(new DrawCursorVisitor() { ctx = ctx, m = m, pos = CursorPosition.Value });
		}

		static void FillInplaceHightlightRectangle(DrawContext ctx, RectangleF rect, Brush brush)
		{
			using (GraphicsPath path = DrawingUtils.RoundRect(
					RectangleF.Inflate(rect, 2, 0), 3))
			{
				ctx.Canvas.SmoothingMode = SmoothingMode.AntiAlias;
				ctx.Canvas.FillPath(brush, path);
				ctx.Canvas.SmoothingMode = SmoothingMode.Default;
			}
		}

		void DrawStringWithInplaceHightlight(MessageBase msg, bool showRawMessages, int msgLineIndex, Font font, Brush brush, PointF location, StringFormat format)
		{
			var textToDisplay = Presenter.GetTextToDisplay(msg, showRawMessages);
			var text = textToDisplay.Text;
			var line = textToDisplay.GetNthTextLine(msgLineIndex);

			int lineBegin = line.StartIndex - text.StartIndex;
			int lineEnd = lineBegin + line.Length;
			DoInplaceHighlighting(msg, font, location, format, text, lineBegin, lineEnd, InplaceHighlightHandler1, ctx.InplaceHightlightBackground1);
			DoInplaceHighlighting(msg, font, location, format, text, lineBegin, lineEnd, InplaceHighlightHandler2, ctx.InplaceHightlightBackground2);

			ctx.Canvas.DrawString(line.Value, font, brush, location, format);
		}

		private void DoInplaceHighlighting(
			MessageBase msg, 
			Font font, 
			PointF location, 
			StringFormat format, 
			StringSlice text, 
			int lineBegin, int lineEnd,
			Func<MessageBase, IEnumerable<Tuple<int, int>>> handler,
			Brush brush)
		{
			if (handler != null)
			{
				foreach (var hlRange in handler(msg))
				{
					int? hlBegin = null;
					int? hlEnd = null;
					if (hlRange.Item1 >= lineBegin && hlRange.Item1 <= lineEnd)
						hlBegin = hlRange.Item1;
					if (hlRange.Item2 >= lineBegin && hlRange.Item2 <= lineEnd)
						hlEnd = hlRange.Item2;
					if (hlBegin != null || hlEnd != null)
					{
						var tmp = DrawingUtils.GetTextSubstringBounds(
							ctx.Canvas, m.MessageRect, text.Value, hlBegin.GetValueOrDefault(lineBegin), hlEnd.GetValueOrDefault(lineEnd),
							font, location.X, format);
						tmp.Inflate(0, -1);
						FillInplaceHightlightRectangle(ctx, tmp, brush);
					}
				}
			}
		}
	};

	internal abstract class MessageTextHandlingVisitor : IMessageBaseVisitor
	{
		public DrawContext ctx;
		public DrawingUtils.Metrics m;

		protected abstract void HandleMessageText(MessageBase msg, float textXPos);

		public virtual void Visit(Content msg)
		{
			HandleMessageText(msg, 0);
		}

		public virtual void Visit(FrameBegin msg)
		{
			HandleMessageText(msg,
				ctx.CharSize.Width * (FrameBegin.GetCollapseMark(msg.Collapsed).Length + 1));
		}

		public virtual void Visit(FrameEnd msg)
		{
			HandleMessageText(msg, ctx.CharSize.Width * 5);
		}

	};

	internal class DrawCursorVisitor : MessageTextHandlingVisitor
	{
		public Presenters.LogViewer.CursorPosition pos;

		protected override void HandleMessageText(MessageBase msg, float textXPos)
		{
			DrawContext dc = ctx;

			var line = dc.GetTextToDisplay(msg).GetNthTextLine(pos.TextLineIndex);
			var lineCharIdx = pos.LineCharIndex;
			RectangleF tmp = DrawingUtils.GetTextSubstringBounds(
				dc.Canvas, m.MessageRect, line.Value + '*',
				lineCharIdx, lineCharIdx + 1, dc.Font,
				m.OffsetTextRect.X + textXPos, ctx.TextFormat);

			dc.Canvas.DrawLine(dc.CursorPen, tmp.X, tmp.Top, tmp.X, tmp.Bottom);
		}
	};

	internal class HitTestingVisitor : MessageTextHandlingVisitor
	{
		public int TextLineIndex;
		public int ClickedPointX;
		public int LineTextPosition;

		public HitTestingVisitor(DrawContext dc, DrawingUtils.Metrics mtx, int clieckedPointX, int lineIndex)
		{
			ctx = dc;
			ClickedPointX = clieckedPointX;
			m = mtx;
			TextLineIndex = lineIndex;
		}

		protected override void HandleMessageText(MessageBase msg, float textXPos)
		{
			DrawContext dc = ctx;
			LineTextPosition = DrawingUtils.ScreenPositionToMessageTextCharIndex(dc.Canvas, msg, dc.ShowRawMessages, TextLineIndex, dc.Font, dc.TextFormat,
				(int)(ClickedPointX - textXPos - m.OffsetTextRect.X));
		}
	};

	public class DrawContext
	{
		public Presenter Presenter;
		public SizeF CharSize;
		public double CharWidthDblPrecision;
		public int LineHeight;
		public int TimeAreaSize;
		public Brush InfoMessagesBrush;
		public Font Font;
		public Brush CommentsBrush;
		public Brush DefaultBackgroundBrush;
		public Pen OutlineMarkupPen, SelectedOutlineMarkupPen;
		public Brush SelectedBkBrush;
		public Brush SelectedFocuslessBkBrush;
		public Brush SelectedTextBrush;
		public Brush SelectedFocuslessTextBrush;
		public Brush HighlightBrush;
		public Brush FocusedMessageBkBrush;
		public Image ErrorIcon, WarnIcon, BookmarkIcon, SmallBookmarkIcon, FocusedMessageIcon, FocusedMessageSlaveIcon;
		public Pen HighlightPen;
		public Pen CursorPen;
		public Pen TimeSeparatorLine;
		public StringFormat TextFormat;
		public Brush InplaceHightlightBackground1;
		public Brush InplaceHightlightBackground2;
		public Cursor RightCursor;
		public Size BackBufferCanvasSize;
		public BufferedGraphics BackBufferCanvas;
		public Graphics Canvas { get { return BackBufferCanvas.Graphics; } }

		public bool ShowTime { get { return Presenter != null ? Presenter.ShowTime : false; } }
		public bool ShowMilliseconds { get { return Presenter != null ? Presenter.ShowMilliseconds : false; } }
		public bool ShowRawMessages { get { return Presenter != null ? Presenter.ShowRawMessages : false; } }
		public SelectionInfo NormalizedSelection { get { return Presenter != null ? Presenter.Selection.Normalize() : new SelectionInfo(); } }
		public Presenters.LogViewer.ColoringMode Coloring { get { return Presenter != null ? Presenter.Coloring : Presenters.LogViewer.ColoringMode.None; } }

		public bool CursorState;
		public Point ScrollPos;
		public Rectangle ClientRect;

		public int SlaveMessagePositionAnimationStep;

		public Point GetTextOffset(int level, int displayIndex)
		{
			int x = FixedMetrics.CollapseBoxesAreaSize + FixedMetrics.LevelOffset * level - ScrollPos.X;
			if (ShowTime)
				x += TimeAreaSize;
			int y = displayIndex * LineHeight - ScrollPos.Y;
			return new Point(x, y);
		}
		public StringUtils.MultilineText GetTextToDisplay(MessageBase msg)
		{
			return Presenter != null ? Presenter.GetTextToDisplay(msg) : msg.TextAsMultilineText;
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

		public static Metrics GetMetrics(Presenter.DisplayLine line, DrawContext dc)
		{
			Point offset = dc.GetTextOffset(line.Message.Level, line.DisplayLineIndex);

			Metrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				dc.ClientRect.Width,
				dc.LineHeight
			);

			m.TimePos = new Point(
				FixedMetrics.CollapseBoxesAreaSize - dc.ScrollPos.X,
				m.MessageRect.Y
			);

			int charCount = dc.GetTextToDisplay(line.Message).GetNthTextLine(line.TextLineIndex).Length;

			m.OffsetTextRect = new Rectangle(
				offset.X,
				m.MessageRect.Y,
				(int)((double)charCount * dc.CharWidthDblPrecision),
				m.MessageRect.Height
			);

			m.OulineBoxCenter = new Point(
				line.Message.IsBookmarked ?
					FixedMetrics.OutlineBoxSize / 2 + 1 :
					FixedMetrics.CollapseBoxesAreaSize / 2,
				m.MessageRect.Y + dc.LineHeight / 2
			);
			m.OulineBox = new Rectangle(
				m.OulineBoxCenter.X - FixedMetrics.OutlineBoxSize / 2,
				m.OulineBoxCenter.Y - FixedMetrics.OutlineBoxSize / 2,
				FixedMetrics.OutlineBoxSize,
				FixedMetrics.OutlineBoxSize
			);

			return m;
		}

		public static RectangleF GetTextSubstringBounds(Graphics g, RectangleF messageRect,
			string msg, int substringBegin, int substringEnd, Font font, float textDrawingXPosition, StringFormat format)
		{
			format.SetMeasurableCharacterRanges(new CharacterRange[] { 
				new CharacterRange(substringBegin, substringEnd - substringBegin) 
			});
			var regions = g.MeasureCharacterRanges(msg, font, new RectangleF(0, 0, 100500, 100000), format);
			var bounds = regions[0].GetBounds(g);
			regions[0].Dispose();
			return new RectangleF(textDrawingXPosition + bounds.X, messageRect.Top, bounds.Width, messageRect.Height);
		}

		public static int ScreenPositionToMessageTextCharIndex(Graphics g, 
			MessageBase msg, bool showRawMessages, int textLineIndex, Font font, StringFormat format, int screenPosition)
		{
			var textToDisplay = Presenter.GetTextToDisplay(msg, showRawMessages);
			var txt = textToDisplay.Text;
			var line = textToDisplay.GetNthTextLine(textLineIndex);
			var lineValue = line.Value;
			int lineCharIdx = ListUtils.BinarySearch(new ListUtils.VirtualList<int>(lineValue.Length, i => i), 0, lineValue.Length, i =>
			{
				format.SetMeasurableCharacterRanges(new CharacterRange[] { new CharacterRange(i, 1) });
				var regions = g.MeasureCharacterRanges(lineValue, font, new RectangleF(0, 0, 100500, 100000), format);
				var charBounds = regions[0].GetBounds(g);
				regions[0].Dispose();
				return ((charBounds.Left + charBounds.Right) / 2) < screenPosition;
			});
			//return (line.StartIndex + lineCharIdx) - txt.StartIndex;
			return lineCharIdx;
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
