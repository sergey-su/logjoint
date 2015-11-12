using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.Timeline;
using LogJoint.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Rectangle = System.Drawing.Rectangle;
using PointF = System.Drawing.PointF;
using Point = System.Drawing.Point;
using SizeF = System.Drawing.SizeF;
using Size = System.Drawing.Size;

namespace LogJoint.UI.Timeline
{
	struct Metrics
	{
		public Rectangle Client;
		public Rectangle TopDrag;
		public Rectangle TopDate;
		public Rectangle TimeLine;
		public Rectangle BottomDate;
		public Rectangle BottomDrag;
	};

	class ControlDrawing
	{
		GraphicsResources res;

		public ControlDrawing(GraphicsResources res)
		{
			this.res = res;
		}

		public void FillBackground(Graphics g, RectangleF dirtyRect)
		{
			g.FillRectangle(res.Background, dirtyRect);
		}

		public void DrawSources(Graphics g, DrawInfo drawInfo)
		{
			foreach (var src in drawInfo.Sources)
			{
				int srcX = src.X;
				int srcRight = src.Right;
				int y1 = src.AvaTimeY1;
				int y2 = src.AvaTimeY2;
				int y3 = src.LoadedTimeY1;
				int y4 = src.LoadedTimeY2;

				// I pass DateRange.End property to calculate bottom Y-coords of the ranges (y2, y4).
				// DateRange.End is past-the-end visible, it is 'maximim-date-belonging-to-range' + 1 tick.
				// End property yelds to the Y-coord that is 1 pixel greater than the Y-coord
				// of 'maximim-date-belonging-to-range' would be. To fix the problem we need 
				// a little correcion (bottomCoordCorrection).
				// I could use DateRange.Maximum but DateRange.End handles better the case 
				// when the range is empty.
				int endCoordCorrection = -1;

				int sourceBarWidth = GetSourceBarWidth(srcX, srcRight);

				Rectangle shadowOuterRect = new Rectangle(
					srcX + StaticMetrics.SourceShadowSize.Width,
					y1 + StaticMetrics.SourceShadowSize.Height,
					sourceBarWidth + 1, // +1 because DrawShadowRect works with rect bounds similarly to FillRectange: it doesn't fill Left+Width row of pixels.
					y2 - y1 + endCoordCorrection + 1
				);

				if (DrawShadowRect.IsValidRectToDrawShadow(shadowOuterRect))
				{
					res.SourcesShadow.Draw(
						g,
						shadowOuterRect,
						ShadowSide.All
					);
				}

				// Draw the source with its native color
				using (Brush sb = new Brush(src.Color.ToColor()))
				{
					DrawTimeLineRange(g, y1, y2 + endCoordCorrection, srcX, sourceBarWidth, sb, res.SourcesBorderPen);
				}

				// Draw the loaded range with a bit darker color
				using (Brush sb = new Brush(src.Color.MakeDarker(16).ToColor()))
				{
					DrawTimeLineRange(g, y3, y4 + endCoordCorrection, srcX, sourceBarWidth, sb, res.SourcesBorderPen);
				}


				foreach (var gap in src.Gaps)
				{
					int gy1 = gap.Y1;
					int gy2 = gap.Y2;

					gy1 += StaticMetrics.MinimumTimeSpanHeight / 2;
					gy2 -= StaticMetrics.MinimumTimeSpanHeight / 2;

					g.FillRectangle(
						res.Background,
						srcX,
						gy1,
						srcRight - srcX + 1,
						gy2 - gy1 + endCoordCorrection + 1
					);

					int tempRectHeight = DrawShadowRect.MinimumRectSize.Height + 1;
					Rectangle shadowTmp = new Rectangle(
						shadowOuterRect.X,
						gy1 - tempRectHeight + StaticMetrics.SourceShadowSize.Height + 1,
						shadowOuterRect.Width,
						tempRectHeight
					);

					if (DrawShadowRect.IsValidRectToDrawShadow(shadowTmp))
					{
						res.SourcesShadow.Draw(g, shadowTmp, ShadowSide.Bottom | ShadowSide.Middle | ShadowSide.Right);
					}

					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy1, res);
					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy2, res);
				}
			}
		}

		public void DrawRulers(Graphics g, Metrics m, DrawInfo drawInfo)
		{
			g.PushState();
			g.EnableTextAntialiasing(true);

			foreach (var rm in drawInfo.RulerMarks)
			{
				int y = rm.Y;
				g.DrawLine(rm.IsMajor ? res.RulersPen2 : res.RulersPen1, 0, y, m.Client.Width, y);
				if (rm.Label != null)
				{
					g.DrawString(rm.Label, res.RulersFont, res.RulersBrush1, 3 + 1, y + 1);
					g.DrawString(rm.Label, res.RulersFont, res.RulersBrush2, 3, y);
				}
			}

			g.PopState();
		}

		public void DrawBookmarks(Graphics g, Metrics m, DrawInfo di)
		{
			foreach (var bmk in di.Bookmarks)
			{
				int y = bmk.Y;
				bool hidden = bmk.IsHidden;
				Pen bookmarkPen = res.BookmarkPen;
				if (hidden)
					bookmarkPen = res.HiddenBookmarkPen;
				g.DrawLine(bookmarkPen, m.Client.Left, y, m.Client.Right, y);
				Image img = res.BookmarkImage;
				g.DrawImage(img,
					m.Client.Right - img.Width - 2,
					y - 2,
					img.Width,
					img.Height
				);
			}
		}

		public void DrawDragAreas(Graphics g, Metrics m, DrawInfo di)
		{
			g.FillRectangle(res.DragAreaBackgroundBrush, new Rectangle(
				0, m.TopDrag.Y, m.Client.Width, m.TopDate.Bottom - m.TopDrag.Y
			));
			DrawDragArea(g, di.TopDragArea, m.Client.Left, m.Client.Right, m.TopDate.Top);
			LogJoint.Drawing.DrawingUtils.DrawDragEllipsis(g, m.TopDrag);

			g.FillRectangle(res.DragAreaBackgroundBrush, new Rectangle(
				0, m.BottomDate.Y, m.Client.Width, m.BottomDrag.Bottom - m.BottomDate.Y
			));
			DrawDragArea(g, di.BottomDragArea, m.Client.Left, m.Client.Right, m.BottomDate.Top);
			LogJoint.Drawing.DrawingUtils.DrawDragEllipsis(g, m.BottomDrag);
		}

		public void DrawCurrentViewTime(Graphics g, Metrics m, DrawInfo di)
		{
			if (di.CurrentTime != null)
			{
				int y = di.CurrentTime.Value.Y;
				g.DrawLine(res.CurrentViewTimePen, m.Client.Left, y, m.Client.Right, y);

				var currSrc = di.CurrentTime.Value.CurrentSource;
				if (currSrc != null)
				{
					int srcX = currSrc.Value.X;
					int srcRight = currSrc.Value.Right;
					g.FillRectangle(res.CurrentViewTimeBrush, new Rectangle(srcX, y - 1, srcRight - srcX, 3));
				}
			}
		}

		public void DrawHotTrackRange(Graphics g, Metrics m, DrawInfo di)
		{
			if (di.HotTrackRange == null)
				return;
			var htr = di.HotTrackRange.Value;
			int x1 = htr.X1;
			int x2 = htr.X2;
			int y1 = htr.Y1;
			int y2 = htr.Y2;
			Rectangle rect = new Rectangle(x1, y1, GetSourceBarWidth(x1, x2), y2 - y1);
			rect.Inflate(1, 1);
			g.DrawRectangle(res.HotTrackRangePen, rect);
			g.FillRectangle(res.HotTrackRangeBrush, rect);
		}


		public void DrawHotTrackDate(Graphics g, Metrics m, DrawInfo di)
		{
			if (di.HotTrackDate == null)
				return;
			int y = di.HotTrackDate.Value.Y;
			g.PushState();
			g.EnableAntialiasing(true);
			g.TranslateTransform(0, y);
			g.DrawLine(res.HotTrackLinePen, 0, 0, m.TimeLine.Right, 0);
			g.FillPolygon(Brushes.Red, res.HotTrackMarker);
			g.TranslateTransform(m.TimeLine.Width - 1, 0);
			g.ScaleTransform(-1, 1, System.Drawing.Drawing2D.MatrixOrder.Prepend);
			g.FillPolygon(Brushes.Red, res.HotTrackMarker);
			g.PopState();
		}

		public void DrawDragArea(Graphics g, DragAreaDrawInfo di, int x1, int x2, int y)
		{
			int center = (x1 + x2) / 2;
			string fullTimestamp = di.LongText;
			if (g.MeasureString(fullTimestamp, res.MainFont).Width < (x2 - x1))
				g.DrawString(fullTimestamp,
					res.MainFont, res.DragAreaTextBrush, center, y, res.CenteredFormat);
			else
				g.DrawString(di.ShortText,
					res.MainFont, res.DragAreaTextBrush, center, y, res.CenteredFormat);
		}


		static int GetSourceBarWidth(int srcLeft, int srcRight)
		{
			int sourceBarWidth = srcRight - srcLeft - StaticMetrics.SourceShadowSize.Width;
			return sourceBarWidth;
		}

		static void DrawTimeLineRange(Graphics g, int y1, int y2, int x1, int width, Brush brush, Pen pen)
		{
			ApplyMinDispayHeight(ref y1, ref y2);

			int radius = 3;

			if (y2 - y1 < radius * 2
			 || width < radius * 2)
			{
				g.FillRectangle(brush, x1, y1, width, y2 - y1);
				g.DrawRectangle(pen, x1, y1, width, y2 - y1);
			}
			else
			{
				var r = new Rectangle(x1, y1, width, y2 - y1);
				g.PushState();
				g.EnableAntialiasing(true);
				g.FillRoundRectangle(brush, r, radius);
				g.DrawRoundRectangle(pen, r, radius);
				g.PopState();
			}
		}

		static void ApplyMinDispayHeight(ref int y1, ref int y2)
		{
			int minRangeDispayHeight = 4;
			if (y2 - y1 < minRangeDispayHeight)
			{
				y1 -= minRangeDispayHeight / 2;
				y2 += minRangeDispayHeight / 2;
			}
		}

		static void DrawCutLine(Graphics g, int x1, int x2, int y, GraphicsResources res)
		{
			g.DrawLine(res.CutLinePen, x1, y - 1, x2, y - 1);
			g.DrawLine(res.CutLinePen, x1 + 2, y, x2 + 1, y);
		}
	};
}
