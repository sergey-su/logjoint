using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Drawing;


namespace LogJoint.UI.LogViewer
{
	internal struct MessageDrawing
	{
		private GraphicsResources resources;
		private ViewDrawing viewDrawing;
		private ViewLineMetrics m;
		private ViewLine msg;
		private Graphics canvas;
		private bool darkMode;

		public static void Draw(
			ViewLine msg,
			GraphicsResources ctx,
			ViewDrawing metrics,
			Graphics canvas,
			bool controlIsFocused,
			out int right,
			bool drawText,
			bool darkMode
		)
		{
			var helper = new MessageDrawing
			{
				resources = ctx,
				msg = msg,
				viewDrawing = metrics,
				canvas = canvas,
				m = metrics.GetMetrics(msg),
				darkMode = darkMode
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
				canvas.DrawString(
					msg.Time,
					resources.Font,
					resources.DefaultForegroundBrush,
					m.TimePos.X, m.TimePos.Y);
			}
		}

		void FillOutlineBackground()
		{
			canvas.FillRectangle(resources.DefaultBackgroundBrush, new Rectangle(0,
				m.MessageRect.Y, viewDrawing.ServiceInformationAreaSize, m.MessageRect.Height));
		}

		void DrawContentOutline()
		{
			Image icon = null;
			Image icon2 = null;
			if (msg.Severity != SeverityIcon.None)
			{
				if (msg.Severity == SeverityIcon.Error)
					icon = resources.ErrorIcon;
				else if (msg.Severity == SeverityIcon.Warning)
					icon = resources.WarnIcon;
			}
			if (msg.IsBookmarked)
				if (icon == null)
					icon = resources.BookmarkIcon;
				else
					icon2 = resources.BookmarkIcon;
			if (icon == null)
				return;
			int w = viewDrawing.ServiceInformationAreaSize;
			var iconSz = icon.GetSize(width: icon2 == null ? (icon == resources.BookmarkIcon ? 12 : 10) : 9).Scale(viewDrawing.DpiScale);
			canvas.DrawImage(icon,
				icon2 == null ? (w - iconSz.Width) / 2 : 1,
				m.MessageRect.Y + (viewDrawing.LineHeight - iconSz.Height) / 2,
				iconSz.Width,
				iconSz.Height
			);
			if (icon2 != null)
			{
				var icon2Sz = icon2.GetSize(width: 9).Scale(viewDrawing.DpiScale);
				canvas.DrawImage(icon2,
					iconSz.Width + 2,
					m.MessageRect.Y + (viewDrawing.LineHeight - icon2Sz.Height) / 2,
					icon2Sz.Width,
					icon2Sz.Height
				);
			}
		}
		
		void DrawMessageBackground()
		{
			Rectangle r = m.MessageRect;
			r.Offset(viewDrawing.ServiceInformationAreaSize, 0);
			Brush b = null;
			Brush tmpBrush = null;

			if (!darkMode && msg.ContextColor != null)
				b = tmpBrush = new Brush (msg.ContextColor.Value);
			else
				b = resources.DefaultBackgroundBrush;
			canvas.FillRectangle(b, r);

			if (msg.SelectedBackground.HasValue)
			{
				RectangleF tmp = GetLineSubstringBounds(
					msg.TextLineValue, msg.SelectedBackground.Value.Item1, msg.SelectedBackground.Value.Item2,
					canvas, resources.Font, this.resources.TextFormat,
					m.MessageRect, m.OffsetTextRect.X);
				canvas.FillRectangle(resources.SelectedBkBrush, tmp);
			}

			if (viewDrawing.TimeAreaSize > 0)
			{
				float x = viewDrawing.ServiceInformationAreaSize + viewDrawing.TimeAreaSize - viewDrawing.ScrollPosX - 2;
				if (x > viewDrawing.ServiceInformationAreaSize)
					canvas.DrawLine(this.resources.TimeSeparatorLine, x, m.MessageRect.Y, x, m.MessageRect.Bottom);
			}

			tmpBrush?.Dispose();
		}

		void DrawMessageSeparator()
		{
			if (msg.HasMessageSeparator)
			{
				float y = m.MessageRect.Bottom - 0.5f;
				canvas.DrawLine(resources.TimeSeparatorLine, m.MessageRect.X, y, m.MessageRect.Right, y);
			}
		}

		void DrawCursorIfNeeded(bool controlIsFocused)
		{
			if (!controlIsFocused || msg.CursorCharIndex == null || !msg.CursorVisible)
				return;

			var lineValue = msg.TextLineValue;
			var lineCharIdx = msg.CursorCharIndex.Value;

			if (lineCharIdx > lineValue.Length)
				return; // defensive measure to avoid crash in UI thread

			RectangleF tmp = GetLineSubstringBounds(
				lineValue + '*', lineCharIdx, lineCharIdx + 1,
				canvas, resources.Font, resources.TextFormat,
				m.MessageRect, m.OffsetTextRect.X
			);

			canvas.DrawLine(resources.CursorPen, tmp.X, tmp.Top, tmp.X, tmp.Bottom);
		}

		static RectangleF GetLineSubstringBounds(
			string line, int lineSubstringBegin, int lineSubstringEnd,
			Graphics g, Font font, StringFormat format,
			RectangleF messageRect, float textDrawingXPosition
		)
		{
			var bounds = g.MeasureCharacterRange(line, font, format, new CharacterRange(lineSubstringBegin, lineSubstringEnd - lineSubstringBegin));

			return new RectangleF(textDrawingXPosition + bounds.X, messageRect.Top, bounds.Width, messageRect.Height);
		}

		static void FillInplaceHightlightRectangle(Graphics ctx, RectangleF rect, Brush brush)
		{
			ctx.PushState();
			ctx.EnableAntialiasing(true);
			ctx.FillRoundRectangle(brush, RectangleF.Inflate(rect, 2, 0), 3);
			ctx.PopState();
		}

		void DrawStringWithInplaceHightlight(bool drawText)
		{
			PointF location = m.OffsetTextRect.Location;
			var lineValue = msg.TextLineValue;

			DoInplaceHighlighting(lineValue, location, msg.SearchResultHighlightingRanges, resources.SearchResultHighlightingBackground);
			DoInplaceHighlighting(lineValue, location, msg.SelectionHighlightingRanges, resources.SelectionHighlightingBackground);
			DoInplaceHighlighting(lineValue, location, msg.HighlightingFiltersHighlightingRanges, null);

			if (drawText)
			{
				Brush tmpBrush = null;
				Brush b;

				if (darkMode && msg.ContextColor != null)
					b = tmpBrush = new Brush(msg.ContextColor.Value);
				else
					b = resources.DefaultForegroundBrush;

				canvas.DrawString(lineValue, resources.Font, b, location, resources.TextFormat);

				tmpBrush?.Dispose();
			}
		}

		private void DoInplaceHighlighting(
			string lineValue,
			PointF location,
			IEnumerable<(int, int, Color)> ranges,
			Brush forcedBrush)
		{
			if (ranges != null)
			{
				foreach (var hlRange in ranges)
				{
					var tmp = GetLineSubstringBounds(
						lineValue,
						hlRange.Item1,
						hlRange.Item2,
						canvas,
						resources.Font,
						resources.TextFormat, 
						m.MessageRect, location.X);
					tmp.Inflate(0, -1);
					if (forcedBrush == null)
					{
						using (var tmpBrush = new Brush(hlRange.Item3))
						{
							FillInplaceHightlightRectangle(canvas, tmp, tmpBrush);
						}
					}
					else
					{
						FillInplaceHightlightRectangle(canvas, tmp, forcedBrush);
					}
				}
			}
		}
	};
}
