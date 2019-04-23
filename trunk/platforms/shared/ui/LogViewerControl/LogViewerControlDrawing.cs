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
using Color = System.Drawing.Color;


namespace LogJoint.UI
{
	internal struct MessageDrawing
	{
		private DrawContext ctx;
		private DrawingUtils.Metrics m;
		private ViewLine msg;
		private IViewModel viewModel;

		public static void Draw(
			ViewLine msg,
			DrawContext ctx,
			IViewModel viewModel,
			bool controlIsFocused,
			out int right,
			bool drawText
		)
		{
			var helper = new MessageDrawing
			{
				ctx = ctx,
				msg = msg,
				viewModel = viewModel,
				m = DrawingUtils.GetMetrics(msg, ctx),
			};

			helper.DrawMessageBackground();

			helper.DrawTime();

			helper.DrawStringWithInplaceHightlight(drawText);

			helper.DrawCursorIfNeeded(controlIsFocused);

			helper.FillOutlineBackground();
			helper.DrawContentOutline();

			helper.DrawMessageSeparator();

			right = helper.m.OffsetTextRect.Right;
		}

		void DrawTime()
		{
			if (msg.Time != null)
			{
				ctx.Canvas.DrawString(
					msg.Time,
					ctx.Font,
					ctx.InfoMessagesBrush,
					m.TimePos.X, m.TimePos.Y);
			}
		}

		void FillOutlineBackground()
		{
			ctx.Canvas.FillRectangle(ctx.DefaultBackgroundBrush, new Rectangle(0,
				m.MessageRect.Y, ctx.CollapseBoxesAreaSize, m.MessageRect.Height));
		}

		void DrawContentOutline()
		{
			Image icon = null;
			Image icon2 = null;
			if (msg.Severity != SeverityIcon.None)
			{
				if (msg.Severity == SeverityIcon.Error)
					icon = ctx.ErrorIcon;
				else if (msg.Severity == SeverityIcon.Warning)
					icon = ctx.WarnIcon;
			}
			if (msg.IsBookmarked)
				if (icon == null)
					icon = ctx.BookmarkIcon;
				else
					icon2 = ctx.BookmarkIcon;
			if (icon == null)
				return;
			int w = ctx.CollapseBoxesAreaSize;
			var iconSz = icon.GetSize(width: icon2 == null ? (icon == ctx.BookmarkIcon ? 12 : 10) : 9).Scale(ctx.DpiScale);
			ctx.Canvas.DrawImage(icon,
				icon2 == null ? (w - iconSz.Width) / 2 : 1,
				m.MessageRect.Y + (ctx.LineHeight - iconSz.Height) / 2,
				iconSz.Width,
				iconSz.Height
			);
			if (icon2 != null)
			{
				var icon2Sz = icon2.GetSize(width: 9).Scale(ctx.DpiScale);
				ctx.Canvas.DrawImage(icon2,
					iconSz.Width + 2,
					m.MessageRect.Y + (ctx.LineHeight - icon2Sz.Height) / 2,
					icon2Sz.Width,
					icon2Sz.Height
				);
			}
		}
		
		void DrawMessageBackground()
		{
			DrawContext dc = ctx;
			Rectangle r = m.MessageRect;
			r.Offset(ctx.CollapseBoxesAreaSize, 0);
			Brush b = null;
			Brush tmpBrush = null;

			if (msg.BackgroundColor != null)
				b = tmpBrush = new Brush (msg.BackgroundColor.Value.ToColor ());
			else
				b = dc.DefaultBackgroundBrush;
			dc.Canvas.FillRectangle(b, r);

			if (msg.SelectedBackground.HasValue)
			{
				RectangleF tmp = GetLineSubstringBounds(
					msg.TextLineValue, msg.SelectedBackground.Value.Item1, msg.SelectedBackground.Value.Item2,
					ctx.Canvas, dc.Font, ctx.TextFormat,
					m.MessageRect, m.OffsetTextRect.X);
				dc.Canvas.FillRectangle(dc.SelectedBkBrush, tmp);
			}

			if (ctx.TimeAreaSize > 0)
			{
				float x = ctx.CollapseBoxesAreaSize + ctx.TimeAreaSize - ctx.ScrollPos.X - 2;
				if (x > ctx.CollapseBoxesAreaSize)
					ctx.Canvas.DrawLine(ctx.TimeSeparatorLine, x, m.MessageRect.Y, x, m.MessageRect.Bottom);
			}

			if (tmpBrush != null)
				tmpBrush.Dispose();
		}

		void DrawMessageSeparator()
		{
			if (msg.HasMessageSeparator)
			{
				float y = m.MessageRect.Bottom - 0.5f;
				ctx.Canvas.DrawLine(ctx.TimeSeparatorLine, m.MessageRect.X, y, m.MessageRect.Right, y);
			}
		}

		void DrawCursorIfNeeded(bool controlIsFocused)
		{
			if (!controlIsFocused || msg.CursorCharIndex == null)
				return;

			var lineValue = msg.TextLineValue;
			var lineCharIdx = msg.CursorCharIndex.Value;

			if (lineCharIdx > lineValue.Length)
				return; // defensive measure to avoid crash in UI thread

			RectangleF tmp = GetLineSubstringBounds(
				lineValue + '*', lineCharIdx, lineCharIdx + 1,
				ctx.Canvas, ctx.Font, ctx.TextFormat,
				m.MessageRect, m.OffsetTextRect.X
			);

			ctx.Canvas.DrawLine(ctx.CursorPen, tmp.X, tmp.Top, tmp.X, tmp.Bottom);
		}

		static RectangleF GetLineSubstringBounds(
			string line, int lineSubstringBegin, int lineSubstringEnd,
			Graphics g, Font font, StringFormat format,
			RectangleF messageRect, float textDrawingXPosition
		)
		{
			var bounds = g.MeasureCharacterRange(line, font, format, new System.Drawing.CharacterRange(lineSubstringBegin, lineSubstringEnd - lineSubstringBegin));

			return new RectangleF(textDrawingXPosition + bounds.X, messageRect.Top, bounds.Width, messageRect.Height);
		}

		static void FillInplaceHightlightRectangle(DrawContext ctx, RectangleF rect, Brush brush)
		{
			ctx.Canvas.PushState();
			ctx.Canvas.EnableAntialiasing(true);
			ctx.Canvas.FillRoundRectangle(brush, RectangleF.Inflate(rect, 2, 0), 3);
			ctx.Canvas.PopState();
		}

		void DrawStringWithInplaceHightlight(bool drawText)
		{
			var brush = ctx.InfoMessagesBrush;
			PointF location = m.OffsetTextRect.Location;
			var lineValue = msg.TextLineValue;

			DoInplaceHighlighting(lineValue, location, msg.SearchResultHighlightingRanges, null, ctx.SearchResultHighlightingBackground);
			DoInplaceHighlighting(lineValue, location, msg.SelectionHighlightingRanges, ctx.SelectionHighlightingBackground, null);
			DoInplaceHighlighting(lineValue, location, msg.HighlightingFiltersHighlightingRanges, null, null);

			if (drawText)
				ctx.Canvas.DrawString(lineValue, ctx.Font, brush, location, ctx.TextFormat);
		}

		private void DoInplaceHighlighting(
			string lineValue,
			PointF location,
			IEnumerable<(int, int, FilterAction)> ranges,
			Brush forcedBrush,
			Brush defaultBrush)
		{
			if (ranges != null)
			{
				foreach (var hlRange in ranges)
				{
					var tmp = GetLineSubstringBounds(
						lineValue,
						hlRange.Item1,
						hlRange.Item2,
						ctx.Canvas,
						ctx.Font,
						ctx.TextFormat, 
						m.MessageRect, location.X);
					tmp.Inflate(0, -1);
					if (forcedBrush == null)
					{
						var cl = hlRange.Item3.GetBackgroundColor();
						if (cl != null)
						{
							using (var tmpBrush = new Brush(cl.Value.ToColor()))
							{
								FillInplaceHightlightRectangle(ctx, tmp, tmpBrush);
							}
						}
						else if (defaultBrush != null)
						{
							FillInplaceHightlightRectangle(ctx, tmp, defaultBrush);
						}
					}
					else
					{
						FillInplaceHightlightRectangle(ctx, tmp, forcedBrush);
					}
				}
			}
		}
	};

	public class DrawContext
	{
		public IViewModel Presenter; // todo: do not support null ref
		public SizeF CharSize => fontDependentData().charSize;
		public double CharWidthDblPrecision => fontDependentData().charWidth;
		public int LineHeight => fontDependentData().lineHeight;
		public int TimeAreaSize => timeAreaSize();
		public int CollapseBoxesAreaSize = 25;
		public float DpiScale = 1f;
		public Brush InfoMessagesBrush;
		public Font Font => fontDependentData().font;
		public Brush CommentsBrush;
		public Brush DefaultBackgroundBrush;
		public Brush SelectedBkBrush;
		public Brush SelectedFocuslessBkBrush;
		public Brush SelectedTextBrush;
		public Brush SelectedFocuslessTextBrush;
		public Brush FocusedMessageBkBrush;
		public Image ErrorIcon, WarnIcon, BookmarkIcon, SmallBookmarkIcon, FocusedMessageIcon, FocusedMessageSlaveIcon;
		public Pen CursorPen;
		public Pen TimeSeparatorLine;
		public StringFormat TextFormat;
		public Brush SearchResultHighlightingBackground;
		public Brush SelectionHighlightingBackground;
		public Graphics Canvas;

		public Point ScrollPos;
		public int ViewWidth;

		public int SlaveMessagePositionAnimationStep;

		public Point GetTextOffset(int displayIndex)
		{
			int x = this.CollapseBoxesAreaSize - ScrollPos.X + TimeAreaSize;
			int y = displayIndex * LineHeight - ScrollPos.Y;
			return new Point(x, y);
		}

		public DrawContext(Func<FontData, (Font font, SizeF charSize, double charWidth)> computeFontDependentData)
		{
			timeAreaSize = Selectors.Create(
				() => Presenter?.TimeMaxLength,
				() => CharSize.Width,
				(maxTimeLength, charWidth) => maxTimeLength.GetValueOrDefault() == 0 ? 0 : ((int)Math.Floor(charWidth * maxTimeLength.Value) + 10)
			);
			fontDependentData = Selectors.Create(
				() => Presenter?.Font,
				fontData =>
				{
					var (font, charSize, charWidth) = computeFontDependentData(fontData ?? new FontData());
					var lineHeight = (int)Math.Floor(charSize.Height);
					return (font, charSize, charWidth, lineHeight);
				}
			);
		}

		private Func<int> timeAreaSize;
		private Func<(Font font, SizeF charSize, double charWidth, int lineHeight)> fontDependentData;
	};

	public static class DrawingUtils
	{
		internal struct Metrics
		{
			public Rectangle MessageRect;
			public Point TimePos;
			public Rectangle OffsetTextRect;
		};

		public static Rectangle GetMessageRect(ViewLine line, DrawContext drawContext)
		{
			return GetMetrics(line, drawContext).MessageRect;
		}

		internal static Metrics GetMetrics(ViewLine line, DrawContext dc)
		{
			Point offset = dc.GetTextOffset(line.LineIndex);

			Metrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				dc.ViewWidth,
				dc.LineHeight
			);

			m.TimePos = new Point(
				dc.CollapseBoxesAreaSize - dc.ScrollPos.X,
				m.MessageRect.Y
			);

			int charCount = line.TextLineValue.Length;

			m.OffsetTextRect = new Rectangle(
				offset.X,
				m.MessageRect.Y,
				(int)((double)charCount * dc.CharWidthDblPrecision),
				m.MessageRect.Height
			);

			return m;
		}

		private static int GetClickedCharIndex(ViewLine msg, DrawContext dc, Metrics m, int clieckedPointX)
		{
			return ScreenPositionToMessageTextCharIndex(dc.Canvas, msg, dc.Font, dc.TextFormat,
				(int)(clieckedPointX - m.OffsetTextRect.X));
		}

		private static int ScreenPositionToMessageTextCharIndex(Graphics g, 
			ViewLine msg, Font font, StringFormat format, int screenPosition)
		{
			var lineValue = msg.TextLineValue;
			int lineCharIdx = ListUtils.BinarySearch(new ListUtils.VirtualList<int>(lineValue.Length, i => i), 0, lineValue.Length, i =>
			{
				var charBounds = g.MeasureCharacterRange(lineValue, font, format, new System.Drawing.CharacterRange(i, 1));
				return ((charBounds.Left + charBounds.Right) / 2) < screenPosition;
			});
			//return (line.StartIndex + lineCharIdx) - txt.StartIndex;
			return lineCharIdx;
		}

		private static IEnumerable<ViewLine> GetVisibleMessagesIterator(DrawContext drawContext, IViewModel viewModel, Rectangle viewRect)
		{
			var (begin, end) = GetVisibleMessages(drawContext, viewModel, viewRect);
			for (var i = begin; i < end; ++i)
				yield return viewModel.ViewLines[i];
		}

		private static (int, int) GetVisibleMessages(DrawContext drawContext, IViewModel viewModel, Rectangle viewRect)
		{
			viewRect.Offset(0, drawContext.ScrollPos.Y);

			int begin = viewRect.Y / drawContext.LineHeight;
			int end = viewRect.Bottom / drawContext.LineHeight;

			if ((viewRect.Bottom % drawContext.LineHeight) != 0)
				++end;

			int availableLines = viewModel.ViewLines.Length;
			return (Math.Min(availableLines, begin), Math.Min(availableLines, end));
		}

		public static void PaintControl(DrawContext drawContext, IViewModel viewModel, 
			bool controlIsFocused, Rectangle dirtyRect, out int maxRight, bool drawViewLinesAggregaredText = true)
		{
			maxRight = 0;

			foreach (var vl in GetVisibleMessagesIterator(drawContext, viewModel, dirtyRect))
			{
				MessageDrawing.Draw(vl, drawContext, viewModel, controlIsFocused, out var right, !drawViewLinesAggregaredText);
				maxRight = Math.Max(maxRight, right);
			}

			if (drawViewLinesAggregaredText && viewModel.ViewLines.Length > 0) {
				drawContext.Canvas.DrawString(viewModel.ViewLinesAggregaredText, drawContext.Font, drawContext.InfoMessagesBrush,
					GetMetrics(viewModel.ViewLines[0], drawContext).OffsetTextRect.Location, drawContext.TextFormat);
			}

			DrawFocusedMessageMark(drawContext, viewModel);
		}

		internal static void DrawFocusedMessageMark(DrawContext drawContext, IViewModel viewModel)
		{
			var dc = drawContext;
			Image focusedMessageMark;
			SizeF focusedMessageSz;
			float markYPos;
			var loc = viewModel.FocusedMessageMarkLocation;
			if (loc == null)
			{
				focusedMessageMark = null;
				focusedMessageSz = new SizeF();
				markYPos = 0;
			}
			else if (loc.Length == 1)
			{
				focusedMessageMark = dc.FocusedMessageIcon;
				focusedMessageSz = focusedMessageMark.GetSize(height: 14);
				markYPos = dc.GetTextOffset(loc[0]).Y + (dc.LineHeight - focusedMessageSz.Height) / 2;
			}
			else
			{
				focusedMessageMark = dc.FocusedMessageIcon;
				focusedMessageSz = focusedMessageMark.GetSize(height: 9);
				float yOffset = loc[0] != loc[1] ?
					(dc.LineHeight - focusedMessageSz.Height) / 2 : -focusedMessageSz.Height / 2;
				markYPos = dc.GetTextOffset(loc[0]).Y + yOffset;
			}
			if (focusedMessageMark != null)
			{
				var canvas = drawContext.Canvas;
				canvas.PushState();
				canvas.TranslateTransform(
					drawContext.CollapseBoxesAreaSize - focusedMessageSz.Width / 2 + 1,
					markYPos + focusedMessageSz.Height / 2);
				if (dc.SlaveMessagePositionAnimationStep > 0)
				{
					focusedMessageSz = focusedMessageMark.GetSize(height: 10);
					var factors = new float[] { .81f, 1f, 0.9f, .72f, .54f, .36f, .18f, .09f };
					float factor = 1f + 1.4f * factors[dc.SlaveMessagePositionAnimationStep-1];
					canvas.ScaleTransform(factor, factor);
				}
				dc.Canvas.DrawImage(
					focusedMessageMark, new RectangleF(
						-focusedMessageSz.Width/2,
						-focusedMessageSz.Height/2,
						focusedMessageSz.Width,
						focusedMessageSz.Height
					));
				canvas.PopState();
			}
		}


		public static void MouseDownHelper(
			IViewModel viewModel,
			DrawContext drawContext,
			Rectangle clientRectangle,
			Point pt,
			MessageMouseEventFlag flags,
			out bool captureTheMouse
		)
		{
			captureTheMouse = true;

			if (viewModel != null)
			{
				foreach (var i in GetVisibleMessagesIterator(drawContext, viewModel, clientRectangle))
				{
					var mtx = GetMetrics(i, drawContext);

					// if user clicked line area
					if (mtx.MessageRect.Contains(pt.X, pt.Y))
					{
						var lineTextPosition = GetClickedCharIndex(i, drawContext, mtx, pt.X);
						if ((flags & MessageMouseEventFlag.DblClick) != 0)
						{
							captureTheMouse = false;
						}
						if (pt.X < drawContext.CollapseBoxesAreaSize)
						{
							flags |= MessageMouseEventFlag.OulineBoxesArea;
						}
						viewModel.OnMessageMouseEvent(i, lineTextPosition, flags, pt);
						break;
					}
				}
			}
		}

		public enum CursorType
		{
			Arrow,
			RightToLeftArrow,
			IBeam
		};

		public static void MouseMoveHelper(
			IViewModel viewModel,
			DrawContext drawContext,
			Rectangle clientRectangle,
			Point pt,
			bool isLeftDrag,
			out CursorType newCursor
		)
		{
			newCursor = CursorType.Arrow;

			if (viewModel != null)
			{
				foreach (var i in GetVisibleMessagesIterator(drawContext, viewModel, clientRectangle))
				{
					var mtx = GetMetrics(i, drawContext);

					if (pt.Y >= mtx.MessageRect.Top && pt.Y < mtx.MessageRect.Bottom)
					{
						if (isLeftDrag)
						{
							var lineTextPosition = GetClickedCharIndex(i, drawContext, mtx, pt.X);
							MessageMouseEventFlag flags = MessageMouseEventFlag.ShiftIsHeld 
								| MessageMouseEventFlag.CapturedMouseMove;
							if (pt.X < drawContext.CollapseBoxesAreaSize)
								flags |= MessageMouseEventFlag.OulineBoxesArea;
							viewModel.OnMessageMouseEvent(i, lineTextPosition, flags, pt);
						}
						if (pt.X < drawContext.CollapseBoxesAreaSize)
							newCursor = CursorType.RightToLeftArrow;
						else if (pt.X >= drawContext.GetTextOffset(0).X)
							newCursor = CursorType.IBeam;
						else
							newCursor = CursorType.Arrow;
					}
				}
			}
		}
	};
}
