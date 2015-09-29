using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Rectangle = System.Drawing.Rectangle;
using PointF = System.Drawing.PointF;
using Point = System.Drawing.Point;
using SizeF = System.Drawing.SizeF;
using Size = System.Drawing.Size;


namespace LogJoint.UI
{
	internal class DrawingVisitor : MessageTextHandlingVisitor, IMessageVisitor
	{
		public int DisplayIndex;
		public int TextLineIdx;
		public bool IsBookmarked;
		public Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler1;
		public Func<IMessage, IEnumerable<Tuple<int, int>>> InplaceHighlightHandler2;
		public Presenters.LogViewer.CursorPosition? CursorPosition;


		public override void Visit(IContent msg)
		{
			base.Visit(msg);

			DrawTime(msg);

			DrawStringWithInplaceHightlight(msg, ctx.ShowRawMessages, TextLineIdx, ctx.Font, ctx.InfoMessagesBrush, m.OffsetTextRect.Location,
				ctx.TextFormat);

			DrawCursorIfNeeded(msg);

			FillOutlineBackground();
			DrawContentOutline(msg);
		}

		public override void Visit(IFrameBegin msg)
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

		public override void Visit(IFrameEnd msg)
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

		protected override void HandleMessageText(IMessage msg, float textXPos)
		{
			DrawMessageBackground(msg, textXPos);
		}



		void DrawTime(IMessage msg)
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

		void DrawContentOutline(IContent msg)
		{
			if (this.TextLineIdx != 0)
				return;
			Image icon = null;
			Image icon2 = null;
			if (msg.Severity == SeverityFlag.Error)
				icon = ctx.ErrorIcon;
			else if (msg.Severity == SeverityFlag.Warning)
				icon = ctx.WarnIcon;
			if (IsBookmarked && TextLineIdx == 0)
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

		void DrawFrameBeginOutline(IFrameBegin msg)
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
				if (IsBookmarked)
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

		void DrawFrameEndOutline(IFrameEnd msg)
		{
			if (IsBookmarked && TextLineIdx == 0)
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
		
		void DrawMessageBackground(IMessage msg, float textXPos)
		{
			DrawContext dc = ctx;
			Rectangle r = m.MessageRect;
			r.Offset(FixedMetrics.CollapseBoxesAreaSize, 0);
			Brush b = null;
			Brush tmpBrush = null;

			if (msg.IsHighlighted())
			{
				b = dc.HighlightBrush;
			}
			else if (msg.Thread != null)
			{
				var coloring = dc.Coloring;
				if (coloring == Settings.Appearance.ColoringMode.None)
					b = dc.DefaultBackgroundBrush;
				else if (msg.Thread.IsDisposed)
					b = dc.DefaultBackgroundBrush;
				else if (coloring == Settings.Appearance.ColoringMode.Threads)
					b = tmpBrush = new Brush(msg.Thread.ThreadColor.ToColor());
				else if (coloring == Settings.Appearance.ColoringMode.Sources)
					b = (msg.LogSource == null || msg.LogSource.IsDisposed) ? dc.DefaultBackgroundBrush : (tmpBrush = new Brush(msg.LogSource.Color.ToColor()));
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
				if (selectionStartIdx < selectionEndIdx && selectionStartIdx >= 0)
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

			if (tmpBrush != null)
				tmpBrush.Dispose();
		}

		void DrawCursorIfNeeded(IMessage msg)
		{
			if (!CursorPosition.HasValue)
				return;
			msg.Visit(new DrawCursorVisitor() { ctx = ctx, m = m, pos = CursorPosition.Value });
		}

		static void FillInplaceHightlightRectangle(DrawContext ctx, RectangleF rect, Brush brush)
		{
			/* todo
			using (GraphicsPath path = DrawingUtils.RoundRect(
					RectangleF.Inflate(rect, 2, 0), 3))
			{
				ctx.Canvas.SmoothingMode = SmoothingMode.AntiAlias;
				ctx.Canvas.FillPath(brush, path);
				ctx.Canvas.SmoothingMode = SmoothingMode.Default;
			}
			*/
			ctx.Canvas.FillRectangle(brush, RectangleF.Inflate(rect, 2, 0));
		}

		void DrawStringWithInplaceHightlight(IMessage msg, bool showRawMessages, int msgLineIndex, Font font, Brush brush, PointF location, StringFormat format)
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
			IMessage msg, 
			Font font, 
			PointF location, 
			StringFormat format, 
			StringSlice text, 
			int lineBegin, int lineEnd,
			Func<IMessage, IEnumerable<Tuple<int, int>>> handler,
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

	internal abstract class MessageTextHandlingVisitor : IMessageVisitor
	{
		public DrawContext ctx;
		public DrawingUtils.Metrics m;

		protected abstract void HandleMessageText(IMessage msg, float textXPos);

		public virtual void Visit(IContent msg)
		{
			HandleMessageText(msg, 0);
		}

		public virtual void Visit(IFrameBegin msg)
		{
			HandleMessageText(msg,
				ctx.CharSize.Width * (FrameBegin.GetCollapseMark(msg.Collapsed).Length + 1));
		}

		public virtual void Visit(IFrameEnd msg)
		{
			HandleMessageText(msg, ctx.CharSize.Width * 5);
		}

	};

	internal class DrawCursorVisitor : MessageTextHandlingVisitor
	{
		public Presenters.LogViewer.CursorPosition pos;

		protected override void HandleMessageText(IMessage msg, float textXPos)
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

		protected override void HandleMessageText(IMessage msg, float textXPos)
		{
			DrawContext dc = ctx;
			LineTextPosition = DrawingUtils.ScreenPositionToMessageTextCharIndex(dc.Canvas, msg, dc.ShowRawMessages, TextLineIndex, dc.Font, dc.TextFormat,
				(int)(ClickedPointX - textXPos - m.OffsetTextRect.X));
		}
	};

	public class DrawContext
	{
		public IPresentationDataAccess Presenter;
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
		public Pen CursorPen;
		public Pen TimeSeparatorLine;
		public StringFormat TextFormat;
		public Brush InplaceHightlightBackground1;
		public Brush InplaceHightlightBackground2;
		public Graphics Canvas;

		public bool ShowTime { get { return Presenter != null ? Presenter.ShowTime : false; } }
		public bool ShowMilliseconds { get { return Presenter != null ? Presenter.ShowMilliseconds : false; } }
		public bool ShowRawMessages { get { return Presenter != null ? Presenter.ShowRawMessages : false; } }
		public SelectionInfo NormalizedSelection { get { return Presenter != null ? Presenter.Selection.Normalize() : new SelectionInfo(); } }
		public Settings.Appearance.ColoringMode Coloring { get { return Presenter != null ? Presenter.Coloring : Settings.Appearance.ColoringMode.None; } }

		public bool CursorState;
		public Point ScrollPos;
		public int ViewWidth;

		public int SlaveMessagePositionAnimationStep;

		public Point GetTextOffset(int level, int displayIndex)
		{
			int x = FixedMetrics.CollapseBoxesAreaSize + FixedMetrics.LevelOffset * level - ScrollPos.X;
			if (ShowTime)
				x += TimeAreaSize;
			int y = displayIndex * LineHeight - ScrollPos.Y;
			return new Point(x, y);
		}
		public StringUtils.MultilineText GetTextToDisplay(IMessage msg)
		{
			return Presenter != null ? 
				Presenters.LogViewer.Presenter.GetTextToDisplay(msg, Presenter.ShowRawMessages) : msg.TextAsMultilineText;
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

		public static Metrics GetMetrics(DisplayLine line, DrawContext dc, bool messageIsBookmarked)
		{
			Point offset = dc.GetTextOffset(line.Message.Level, line.DisplayLineIndex);

			Metrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				dc.ViewWidth,
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
				messageIsBookmarked ?
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
			var bounds = g.MeasureCharacterRange(msg, font, format, new System.Drawing.CharacterRange(substringBegin, substringEnd - substringBegin));

			return new RectangleF(textDrawingXPosition + bounds.X, messageRect.Top, bounds.Width, messageRect.Height);
		}

		public static int ScreenPositionToMessageTextCharIndex(Graphics g, 
			IMessage msg, bool showRawMessages, int textLineIndex, Font font, StringFormat format, int screenPosition)
		{
			var textToDisplay = Presenter.GetTextToDisplay(msg, showRawMessages);
			var txt = textToDisplay.Text;
			var line = textToDisplay.GetNthTextLine(textLineIndex);
			var lineValue = line.Value;
			int lineCharIdx = ListUtils.BinarySearch(new ListUtils.VirtualList<int>(lineValue.Length, i => i), 0, lineValue.Length, i =>
			{
				var charBounds = g.MeasureCharacterRange(lineValue, font, format, new System.Drawing.CharacterRange(i, 1));
				return ((charBounds.Left + charBounds.Right) / 2) < screenPosition;
			});
			//return (line.StartIndex + lineCharIdx) - txt.StartIndex;
			return lineCharIdx;
		}

		public static System.Drawing.Drawing2D.GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
		{
			RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
			var path = new System.Drawing.Drawing2D.GraphicsPath();
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
