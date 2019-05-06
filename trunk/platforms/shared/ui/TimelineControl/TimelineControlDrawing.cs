using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.Timeline;
using LogJoint.Drawing;

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
		public int MinMarkHeight;
		public int ContainersHeaderAreaHeight;
		public int ContainerControlSize;

		public Metrics(
			Rectangle clientRect,
			int dateAreaHeight,
			int dragAreaHeight,
			int minMarkHeight,
			int containersHeaderAreaHeight = 10,
			int containerControlSize = 7
		)
		{
			Client = clientRect;
			TopDrag = new Rectangle(dragAreaHeight / 2, 0, 
				Client.Width - dragAreaHeight, dragAreaHeight);
			TopDate = new Rectangle(0, TopDrag.Bottom, Client.Width, dateAreaHeight);
			BottomDrag = new Rectangle(dragAreaHeight / 2, 
				Client.Height - dragAreaHeight, Client.Width - dragAreaHeight, dragAreaHeight);
			BottomDate = new Rectangle(0, BottomDrag.Top - dateAreaHeight, Client.Width, dateAreaHeight);
			TimeLine = new Rectangle(0, TopDate.Bottom + StaticMetrics.SourcesVerticalPadding, Client.Width,
				BottomDate.Top - TopDate.Bottom - 2*StaticMetrics.SourcesVerticalPadding);
			
			MinMarkHeight = minMarkHeight;
			ContainersHeaderAreaHeight = containersHeaderAreaHeight;
			ContainerControlSize = containerControlSize;
		}

		public HitTestResult HitTest(Point pt)
		{
			if (TimeLine.Contains(pt))
				return new HitTestResult() { Area = ViewArea.Timeline };
			else if (TopDate.Contains(pt))
				return new HitTestResult() { Area = ViewArea.TopDate };
			else if (BottomDate.Contains(pt))
				return new HitTestResult() { Area = ViewArea.BottomDate };
			else if (TopDrag.Contains(pt))
				return new HitTestResult() { Area = ViewArea.TopDrag };
			else if (BottomDrag.Contains(pt))
				return new HitTestResult() { Area = ViewArea.BottomDrag };
			else
				return new HitTestResult() { Area = ViewArea.None };
		}

		public PresentationMetrics ToPresentationMetrics()
		{
			return new PresentationMetrics()
			{
				ClientArea = TimeLine,
				DistanceBetweenSources = StaticMetrics.DistanceBetweenSources,
				SourcesHorizontalPadding = StaticMetrics.SourcesHorizontalPadding,
				MinimumTimeSpanHeight = StaticMetrics.MinimumTimeSpanHeight,
				MinMarkHeight = MinMarkHeight,
				ContainersHeaderAreaHeight = ContainersHeaderAreaHeight,
				ContainerControlSize = ContainerControlSize
			};
		}
	};

	class ControlDrawing
	{
		readonly GraphicsResources res;
		readonly IViewModel viewModel;

		public ControlDrawing(GraphicsResources res, IViewModel viewModel)
		{
			this.res = res;
			this.viewModel = viewModel;
		}

		public int MeasureDatesAreaHeight(Graphics g)
		{
			SizeF tmp = g.MeasureString("0123:-", res.MainFont);
			return (int)Math.Ceiling(tmp.Height);
		}

		public void FillBackground(Graphics g, RectangleF dirtyRect)
		{
			g.FillRectangle(res.Background, dirtyRect);
		}

		public void DrawSources(Graphics g, DrawInfo drawInfo)
		{
			bool darkMode = viewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
			foreach (var src in drawInfo.Sources)
			{
				int srcX = src.X;
				int srcRight = src.Right;
				int y1 = src.AvaTimeY1;
				int y2 = src.AvaTimeY2;
				int y3 = src.LoadedTimeY1;
				int y4 = src.LoadedTimeY2;

				// I pass DateRange.End property to calculate bottom Y-coords of the ranges (y2, y4).
				// DateRange.End is past-the-end visible, it is 'maximum-date-belonging-to-range' + 1 tick.
				// End property yields to the Y-coord that is 1 pixel greater than the Y-coord
				// of 'maximum-date-belonging-to-range' would be. To fix the problem we need 
				// a little correction (bottomCoordCorrection).
				// I could use DateRange.Maximum but DateRange.End handles better the case 
				// when the range is empty.
				int endCoordCorrection = -1;

				int sourceBarWidth = GetSourceBarWidth(srcX, srcRight);

				if (darkMode)
				{
					var p = new Pen(src.Color, 2);
					DrawTimeLineRange(g, y1, y2 + endCoordCorrection, srcX, sourceBarWidth, res.DarkModeSourceFillBrush, p);
				}
				else
				{
					using (Brush b = new Brush(src.Color)) // Draw the source with its native color
						DrawTimeLineRange(g, y1, y2 + endCoordCorrection, srcX, sourceBarWidth, b, res.LightModeSourcesBorderPen);
					using (Brush sb = new Brush(src.Color.MakeDarker(16))) // Draw the loaded range with a bit darker color
						if (y3 != y4)
							DrawTimeLineRange(g, y3, y4 + endCoordCorrection, srcX, sourceBarWidth, sb, res.LightModeSourcesBorderPen);
				}


				foreach (var gap in src.Gaps)
				{
					int gy1 = gap.Y1;
					int gy2 = gap.Y2;

					gy1 += StaticMetrics.MinimumTimeSpanHeight / 2;
					gy2 -= StaticMetrics.MinimumTimeSpanHeight / 2;

					g.FillRectangle(
						res.Background,
						srcX - 1,
						gy1,
						srcRight - srcX + 2,
						gy2 - gy1 + endCoordCorrection + 1
					);

					var cutLinePen = darkMode ?
						  new Pen(src.Color, 1f, res.CutLinePenPattern)
						: res.LightModeCutLinePen;
					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy1, cutLinePen);
					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy2, cutLinePen);
				}
			}
		}
		
		public void DrawContainerControls(Graphics g, DrawInfo drawInfo)
		{
			var bounds = drawInfo.ContainerControls.Bounds;
			g.FillRectangle(res.Background, bounds.X, bounds.Y, bounds.Width, bounds.Height);
			foreach (var cc in drawInfo.ContainerControls.Controls)
			{
				if (cc.HintLine.IsVisible)
				{
					var hl = cc.HintLine;
					g.DrawLines(res.ContainerControlHintPen, new []
					{
						new PointF(hl.X1, hl.Bottom),
						new PointF(hl.X1, hl.BaselineY),
						new PointF(hl.X2, hl.BaselineY),
						new PointF(hl.X2, hl.Bottom)
					});
				}
				var ccBoxRect = new RectangleF(
					cc.ControlBox.Bounds.X, cc.ControlBox.Bounds.Y, cc.ControlBox.Bounds.Width, cc.ControlBox.Bounds.Height
				);
				if (cc.HintLine.IsVisible)
				{
					g.FillRectangle(res.Background,
						RectangleF.FromLTRB(ccBoxRect.Left - 1, ccBoxRect.Top, ccBoxRect.Right + 2, ccBoxRect.Bottom));
				}
				g.FillRectangle(res.Background, ccBoxRect);
				g.DrawRectangle(res.ContainerControlHintPen, ccBoxRect);
				var midY = (ccBoxRect.Top + ccBoxRect.Bottom) / 2;
				var midX = (ccBoxRect.Left + ccBoxRect.Right) / 2;
				var pad = (float)Math.Ceiling(ccBoxRect.Width / 6);
				g.DrawLine(
					res.ContainerControlHintPen,
					ccBoxRect.Left + pad, midY,
					ccBoxRect.Right - pad, midY
				);
				if (!cc.ControlBox.IsExpanded)
				{
					g.DrawLine(
						res.ContainerControlHintPen,
						midX, ccBoxRect.Top + pad,
						midX, ccBoxRect.Bottom - pad
					);
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
					int x = 6;
					g.DrawString(rm.Label, res.RulersFont, res.RulersBrush1, x + 1, y + 1);
					g.DrawString(rm.Label, res.RulersFont, res.RulersBrush2, x, y);
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
				if (img == null)
					continue;
				var sz = img.GetSize(height: 5);
				g.DrawImage(img,
					m.Client.Right - sz.Width - 2,
					y - 2,
					sz.Width,
					sz.Height
				);
			}
		}

		public void DrawDragAreas(Graphics g, Metrics m, DrawInfo di)
		{
			g.FillRectangle(res.DragAreaBackgroundBrush, new Rectangle(
				0, m.TopDrag.Y, m.Client.Width, m.TopDate.Bottom - m.TopDrag.Y
			));
			DrawDragArea(g, di.TopDragArea, m.Client.Left, m.Client.Right, m.TopDate.Top);
			if (m.TopDrag.Height > 0)
				LogJoint.Drawing.DrawingUtils.DrawDragEllipsis(g, m.TopDrag);

			g.FillRectangle(res.DragAreaBackgroundBrush, new Rectangle(
				0, m.BottomDate.Y, m.Client.Width, m.BottomDrag.Bottom - m.BottomDate.Y
			));
			DrawDragArea(g, di.BottomDragArea, m.Client.Left, m.Client.Right, m.BottomDate.Top);
			if (m.BottomDrag.Height > 0
			)
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
			g.ScaleTransform(-1, 1);
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
			int sourceBarWidth = srcRight - srcLeft;
			return sourceBarWidth;
		}

		static void DrawTimeLineRange(Graphics g, int y1, int y2, int x1, int width, Brush brush, Pen pen)
		{
			ApplyMinDispayHeight(ref y1, ref y2);

			int radius = 10;

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

		static void DrawCutLine(Graphics g, int x1, int x2, int y, Pen pen)
		{
			g.DrawLine(pen, x1, y - 1, x2, y - 1);
			g.DrawLine(pen, x1 + 2, y, x2 + 1, y);
		}
	};
}
