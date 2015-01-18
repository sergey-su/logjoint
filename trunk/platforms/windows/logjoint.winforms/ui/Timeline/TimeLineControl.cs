using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Timeline;
using LogJoint.UI.Timeline;

namespace LogJoint.UI
{
	public partial class TimeLineControl : Control, IView
	{
		public TimeLineControl()
		{
			InitializeComponent();
			
			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			this.focuslessMouseWheelMessagingFilter = new UIUtils.FocuslessMouseWheelMessagingFilter(this);

			contextMenu.Opened += delegate(object sender, EventArgs e)
			{
				Invalidate();
			};
			contextMenu.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs e)
			{
				Invalidate();
			};
			this.Disposed += (sender, e) => focuslessMouseWheelMessagingFilter.Dispose();
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
			InternalUpdate();
		}

		void IView.Invalidate()
		{
			base.Invalidate();
		}

		void IView.Update()
		{
			InternalUpdate();
		}

		void IView.Zoom(int delta)
		{
			DateTime? curr = host.CurrentViewTime;
			if (!curr.HasValue)
				return;
			ZoomRange(curr.Value, -delta * 10);
		}

		void IView.Scroll(int delta)
		{
			ShiftRange(-delta * 10);
		}

		void IView.ZoomToViewAll()
		{
			DoSetRangeAnimated(availableRange);
			Invalidate();
		}

		public void TrySwitchOnViewTailMode()
		{
			FireNavigateEvent(availableRange.End, NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries, null);
		}

		public void TrySwitchOffViewTailMode()
		{
			FireNavigateEvent(availableRange.End, NavigateFlag.AlignCenter | NavigateFlag.OriginDate, null);
		}

		DateRange IView.TimeRange
		{
			get { return range; }
		}

		bool IView.AreMillisecondsVisible
		{
			get
			{
				return AreMillisecondsVisibleInternal(FindRulerIntervals());
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
				GraphicsPath gp = roundRectsPath;
				gp.Reset();
				UIUtils.AddRoundRect(gp, new Rectangle(x1, y1, width, y2 - y1), radius);
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.FillPath(brush, gp);
				g.DrawPath(pen, gp);
				g.SmoothingMode = SmoothingMode.HighSpeed;
			}
		}

		static void DrawCutLine(Graphics g, int x1, int x2, int y, Resources res)
		{
			g.DrawLine(res.CutLinePen, x1, y - 1, x2, y - 1);
			g.DrawLine(res.CutLinePen, x1 + 2, y, x2 + 1, y);
		}

		struct SourcesDrawHelper
		{
			readonly Rectangle r;
			readonly int sourcesCount;

			public SourcesDrawHelper(Metrics m, int sourcesCount)
			{
				r = m.TimeLine;
				r.Inflate(-StaticMetrics.SourcesHorizontalPadding, 0);
				this.sourcesCount = sourcesCount;
			}

			public bool NeedsDrawing
			{
				get
				{
					if (r.Width == 0) // Nowhere to draw
					{
						return false;
					}
					if (sourcesCount == 0) // Nothing to draw
					{
						return false;
					}
					if (r.Width / sourcesCount < 5) // Too little room to fit all sources. Give up.
					{
						return false;
					}
					return true;
				}
			}

			public int GetSourceLeft(int sourceIdx)
			{
				return r.Left + sourceIdx * r.Width / sourcesCount;
			}

			public int GetSourceRight(int sourceIdx)
			{
				// Left-coord of the next source (sourceIdx + 1)
				int nextSrcLeft = GetSourceLeft(sourceIdx + 1);

				// Right coord of the source
				int srcRight = nextSrcLeft - 1;
				if (sourceIdx != sourcesCount - 1)
					srcRight -= StaticMetrics.DistanceBetweenSources;

				return srcRight;
			}

			public int? XCoordToSourceIndex(int x)
			{
				if (x < r.Left)
					return null;
				int tmp = (x - r.Left) * sourcesCount / r.Width;
				if (x >= GetSourceRight(tmp))
					return null;
				if (tmp >= sourcesCount)
					return null;
				return tmp;
			}

			public int GetSourceBarWidth(int srcLeft, int srcRight)
			{
				int sourceBarWidth = srcRight - srcLeft - StaticMetrics.SourceShadowSize.Width;
				return sourceBarWidth;
			}
		};

		void DrawSources(Graphics g, Metrics m, DateRange drange)
		{
			SourcesDrawHelper helper = new SourcesDrawHelper(m, host.SourcesCount);

			if (!helper.NeedsDrawing)
				return;

			int sourceIdx = 0;
			foreach (var src in host.Sources)
			{
				var gaps = src.TimeGaps.Gaps;

				// Left-coord of this source (sourceIdx)
				int srcX     = helper.GetSourceLeft(sourceIdx);

				// Right coord of the source
				int srcRight = helper.GetSourceRight(sourceIdx);

				DateRange avaTime = src.AvailableTime;
				int y1 = GetYCoordFromDate(m, drange, avaTime.Begin);
				int y2 = GetYCoordFromDate(m, drange, avaTime.End);

				DateRange loadedTime = src.LoadedTime;
				int y3 = GetYCoordFromDate(m, drange, loadedTime.Begin);
				int y4 = GetYCoordFromDate(m, drange, loadedTime.End);

				// I pass DateRange.End property to calculate bottom Y-coords of the ranges (y2, y4).
				// DateRange.End is past-the-end visible, it is 'maximim-date-belonging-to-range' + 1 tick.
				// End property yelds to the Y-coord that is 1 pixel greater than the Y-coord
				// of 'maximim-date-belonging-to-range' would be. To fix the problem we need 
				// a little correcion (bottomCoordCorrection).
				// I could use DateRange.Maximum but DateRange.End handles better the case 
				// when the range is empty.
				int endCoordCorrection = -1;

				int sourceBarWidth = helper.GetSourceBarWidth(srcX, srcRight);
				
				Rectangle shadowOuterRect = new Rectangle(
					srcX + StaticMetrics.SourceShadowSize.Width,
					y1 + StaticMetrics.SourceShadowSize.Height,
					sourceBarWidth + 1, // +1 because DrawShadowRect works with rect bounds similarly to FillRectange: it doesn't fill Left+Width row of pixels.
					y2 - y1 + endCoordCorrection + 1
				);

				if (UIUtils.DrawShadowRect.IsValidRectToDrawShadow(shadowOuterRect))
				{
					res.SourcesShadow.Draw(
						g,
						shadowOuterRect,
						Border3DSide.All
					);
				}

				// Draw the source with its native color
				using (SolidBrush sb = new SolidBrush(src.Color.ToColor()))
				{
					DrawTimeLineRange(g, y1, y2 + endCoordCorrection, srcX, sourceBarWidth, sb, res.SourcesBorderPen);
				}

				// Draw the loaded range with a bit darker color
				using (SolidBrush sb = new SolidBrush(src.Color.MakeDarker(16).ToColor()))
				{
					DrawTimeLineRange(g, y3, y4 + endCoordCorrection, srcX, sourceBarWidth, sb, res.SourcesBorderPen);
				}


				foreach (TimeGap gap in gaps)
				{
					// Ignore irrelevant gaps
					if (!avaTime.IsInRange(gap.Range.Begin))
						continue;

					int gy1 = GetYCoordFromDate(m, drange, gap.Range.Begin);
					int gy2 = GetYCoordFromDate(m, drange, gap.Range.End);

					gy1 += StaticMetrics.MinimumTimeSpanHeight / 2;
					gy2 -= StaticMetrics.MinimumTimeSpanHeight / 2;

					g.FillRectangle(
						res.Background, 
						srcX, 
						gy1, 
						srcRight - srcX + 1, 
						gy2 - gy1 + endCoordCorrection + 1
					);

					int tempRectHeight = UIUtils.DrawShadowRect.MinimumRectSize.Height + 1;
					Rectangle shadowTmp = new Rectangle(
						shadowOuterRect.X,
						gy1 - tempRectHeight + StaticMetrics.SourceShadowSize.Height + 1,
						shadowOuterRect.Width,
						tempRectHeight
					);

					if (UIUtils.DrawShadowRect.IsValidRectToDrawShadow(shadowTmp))
					{
						res.SourcesShadow.Draw(g, shadowTmp, Border3DSide.Bottom | Border3DSide.Middle | Border3DSide.Right);
					}

					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy1, res);
					DrawCutLine(g, srcX, srcX + sourceBarWidth, gy2, res);
				}

				//foreach (IWinFormsTimeLineExtension ext in src.Extensions.Where(baseExt => baseExt is IWinFormsTimeLineExtension))
				//{
				//	TimeLineExtensionLocation loc = ext.GetLocation(sourceBarWidth);
				//	int ey1 = GetYCoordFromDate(m, drange, loc.Dates.Begin);
				//	int ey2 = GetYCoordFromDate(m, drange, loc.Dates.End);
				//	Rectangle extRect = new Rectangle(srcX + loc.xPosition, ey1, loc.Width, ey2 - ey1);
				//	extRect.X += 1;
				//	extRect.Width -= 1;
				//	ext.Draw(g, extRect);
				//}

				++sourceIdx;
			}
		}

		static string GetRulerLabelFormat(RulerMark rm)
		{
			string labelFmt = null;
			switch (rm.Component)
			{
				case DateComponent.Year:
					labelFmt = "yyyy";
					break;
				case DateComponent.Month:
					if (rm.IsMajor)
						labelFmt = "Y"; // year+month
					else
						labelFmt = "MMM";
					break;
				case DateComponent.Day:
					if (rm.IsMajor)
						labelFmt = "m";
					else
						labelFmt = "dd (ddd)";
					break;
				case DateComponent.Hour:
					labelFmt = "t";
					break;
				case DateComponent.Minute:
					labelFmt = "t";
					break;
				case DateComponent.Seconds:
					labelFmt = "T";
					break;
				case DateComponent.Milliseconds:
					labelFmt = "fff";
					break;
			}
			return labelFmt;
		}

		void DrawRulers(Graphics g, Metrics m, DateRange drange, RulerIntervals? rulerIntervals)
		{
			if (!rulerIntervals.HasValue)
				return;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

			foreach (RulerMark rm in presenter.GenerateRulerMarks(rulerIntervals.Value, drange))
			{
				int y = GetYCoordFromDate(m, drange, rm.Time);
				g.DrawLine(rm.IsMajor ? res.RulersPen2 : res.RulersPen1, 0, y, m.Client.Width, y);
				string labelFmt = GetRulerLabelFormat(rm);
				if (labelFmt != null)
				{
					string label = rm.Time.ToString(labelFmt);
					g.DrawString(label, res.RulersFont, Brushes.White, 3 + 1, y + 1);
					g.DrawString(label, res.RulersFont, Brushes.Gray, 3, y);
				}
			}

			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		}

		public void DrawDragArea(Graphics g, DateTime timestamp, int x1, int x2, int y)
		{
			DrawDragArea(g, FindRulerIntervals(), timestamp, x1, x2, y);
		}

		void DrawDragArea(Graphics g, RulerIntervals? rulerIntervals, DateTime timestamp, int x1, int x2, int y)
		{
			int center = (x1 + x2) / 2;
			string fullTimestamp = GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals);
			if (g.MeasureString(fullTimestamp, this.Font).Width < (x2 - x1))
				g.DrawString(fullTimestamp, 
					this.Font, Brushes.Black, center, y, res.CenteredFormat);
			else
				g.DrawString(GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals, false),
					this.Font, Brushes.Black, center, y, res.CenteredFormat);
		}

		void DrawDragAreas(Graphics g, Metrics m, RulerIntervals? rulerIntervals)
		{
			g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
				0, m.TopDrag.Y, m.Client.Width, m.TopDate.Bottom - m.TopDrag.Y
			));
			DrawDragArea(g, rulerIntervals, range.Begin, m.Client.Left, m.Client.Right, m.TopDate.Top);
			UIUtils.DrawDragEllipsis(g, m.TopDrag);

			g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
				0, m.BottomDate.Y, m.Client.Width, m.BottomDrag.Bottom - m.BottomDate.Y
			));
			DrawDragArea(g, rulerIntervals, range.End, m.Client.Left, m.Client.Right, m.BottomDate.Top);
			UIUtils.DrawDragEllipsis(g, m.BottomDrag);
		}

		void DrawBookmarks(Graphics g, Metrics m, DateRange drange)
		{
			foreach (IBookmark bmk in host.Bookmarks)
			{
				DateTime displayBmkTime = bmk.Time.ToLocalDateTime();
				if (!drange.IsInRange(displayBmkTime))
					continue;
				int y = GetYCoordFromDate(m, drange, displayBmkTime);
				bool hidden = false;
				if (bmk.Thread != null)
					if (!bmk.Thread.ThreadMessagesAreVisible)
						hidden = true;
				Pen bookmarkPen = res.BookmarkPen;
				if (hidden)
					bookmarkPen = res.HiddenBookmarkPen;
				g.DrawLine(bookmarkPen, m.Client.Left, y, m.Client.Right, y);
				Image img = this.bookmarkPictureBox.Image;
				g.DrawImage(img,
					m.Client.Right - img.Width - 2,
					y - 2,
					img.Width,
					img.Height
				);
			}
		}

		void DrawCurrentViewTime(Graphics g, Metrics m, DateRange drange)
		{
			DateTime? curr = host.CurrentViewTime;
			if (curr.HasValue && drange.IsInRange(curr.Value))
			{
				int y = GetYCoordFromDate(m, drange, curr.Value);
				g.DrawLine(res.CurrentViewTimePen, m.Client.Left, y, m.Client.Right, y);

				var currentSource = host.CurrentSource;
				if (currentSource != null && host.SourcesCount >= 2)
				{
					SourcesDrawHelper helper = new SourcesDrawHelper(m, host.SourcesCount);
					int sourceIdx = 0;
					foreach (var src in host.Sources)
					{
						if (currentSource == src)
						{
							int srcX = helper.GetSourceLeft(sourceIdx);
							int srcRight = helper.GetSourceRight(sourceIdx);
							g.FillRectangle(res.CurrentViewTimeBrush, new Rectangle(srcX, y - 1, srcRight - srcX, 3));
							break;
						}
						++sourceIdx;
					}
				}
			}
		}

		void DrawHotTrackDate(Graphics g, Metrics m, DateRange drange)
		{
			if (hotTrackDate == null)
				return;
			int y = GetYCoordFromDate(m, drange, hotTrackDate.Value);
			GraphicsState s = g.Save();
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TranslateTransform(0, y);
			g.DrawLine(res.HotTrackLinePen, 0, 0, m.TimeLine.Right, 0);
			g.FillPath(Brushes.Red, res.HotTrackMarker);
			g.TranslateTransform(m.TimeLine.Width - 1, 0);
			g.ScaleTransform(-1, 1, MatrixOrder.Prepend);
			g.FillPath(Brushes.Red, res.HotTrackMarker);
			g.Restore(s);
		}

		void DrawHotTrackRange(Graphics g, Metrics m, DateRange drange)
		{
			if (hotTrackRange.Source == null)
				return;
			SourcesDrawHelper helper = new SourcesDrawHelper(m, host.SourcesCount);
			int x1 = helper.GetSourceLeft(hotTrackRange.SourceIndex.Value);
			int x2 = helper.GetSourceRight(hotTrackRange.SourceIndex.Value);
			DateRange r = (hotTrackRange.Range == null) ? hotTrackRange.Source.AvailableTime : hotTrackRange.Range.Value;
			if (r.IsEmpty)
				return;
			int y1 = GetYCoordFromDate(m, drange, r.Begin);
			int y2 = GetYCoordFromDate(m, drange, r.Maximum);
			if (hotTrackRange.Range != null)
			{
				if (hotTrackRange.RangeBegin != null)
					y1 -= StaticMetrics.MinimumTimeSpanHeight;
				if (hotTrackRange.RangeEnd != null)
					y2 += StaticMetrics.MinimumTimeSpanHeight;
			}
			Rectangle rect = new Rectangle(x1, y1, helper.GetSourceBarWidth(x1, x2), y2 - y1);
			rect.Inflate(1, 1);
			g.DrawRectangle(res.HotTrackRangePen, rect);
			g.FillRectangle(res.HotTrackRangeBrush, rect);
		}

		void DrawFocusRect(Graphics g)
		{
			if (Focused)
			{
				if (host.FocusRectIsRequired)
				{
					ControlPaint.DrawFocusRectangle(g, this.ClientRectangle);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			Graphics g = pe.Graphics;

			// Fill the background
			g.FillRectangle(res.Background, ClientRectangle);

			// Display range. Equal to the animation range in there is active animation.
			DateRange drange = animationRange.GetValueOrDefault(range);

			if (drange.IsEmpty)
				return;

			if (host.SourcesCount == 0)
				return;

			Metrics m = GetMetrics();

			RulerIntervals? rulerIntervals = FindRulerIntervals(m, drange.Length.Ticks);

			DrawSources(g, m, drange);



			DrawRulers(g, m, drange, rulerIntervals);

			DrawDragAreas(g, m, rulerIntervals);

			DrawBookmarks(g, m, drange);

			DrawCurrentViewTime(g, m, drange);

			DrawHotTrackRange(g, m, drange);

			DrawHotTrackDate(g, m, drange);

			DrawFocusRect(g);

			base.OnPaint(pe);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			Invalidate();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			Invalidate();
			base.OnLostFocus(e);
		}

		void UpdateRange()
		{
			DateRange union = DateRange.MakeEmpty();
			foreach (var s in host.Sources)
				union = DateRange.Union(union, s.AvailableTime);
			DateRange newRange;
			if (range.IsEmpty)
			{
				newRange = union;
			}
			else
			{
				DateRange tmp = DateRange.Intersect(union, range);
				if (tmp.IsEmpty)
				{
					newRange = union;
				}
				else
				{
					newRange = new DateRange(
						range.Begin == availableRange.Begin ? union.Begin : tmp.Begin,
						range.End == availableRange.End ? union.End : tmp.End
					);
				}
			}
			DoSetRange(newRange);
			availableRange = union;
		}

		void UpdateDatesSize()
		{
			using (Graphics g = this.CreateGraphics())
			{
				SizeF tmp = g.MeasureString("0123", this.Font);
				datesSize = new Size((int)Math.Ceiling(tmp.Width),
					(int)Math.Ceiling(tmp.Height));
			}
		}

		void InternalUpdate()
		{
			if (IsDisposed)
				return;

			StopDragging(false);

			UpdateRange();
			UpdateDatesSize();

			if (range.IsEmpty)
			{
				RelaseStatusReport();
				ReleaseHotTrack();
			}

			Invalidate();
		}

		public DateRange AvailableTimeRange
		{
			get { return availableRange; }
		}

		static double GetTimeDensity(Metrics m, DateRange range)
		{
			return (double)m.TimeLine.Height / range.Length.TotalMilliseconds;
		}

		static int GetYCoordFromDate(Metrics m, DateRange range, DateTime t)
		{
			return GetYCoordFromDate(m, range, GetTimeDensity(m, range), t);
		}

		static double GetYCoordFromDate_UniformScale(Metrics m, DateRange range, double density, DateTime t)
		{
			TimeSpan pos = t - range.Begin;
			double ret = (double)(m.TimeLine.Top + pos.TotalMilliseconds * density);
			return ret;
		}

		static double GetYCoordFromDate_NonUniformScale(Metrics m, DateRange range, ITimeGaps gaps, double density, DateTime t)
		{
			// Find the closest gap that is located after (or covers) the date in question (t)
			int nextGapIdx = gaps.BinarySearch(0, gaps.Count, delegate(TimeGap g) { return g.Range.End <= t; });

			// Amount of gaps. It defines Y-coordinate offset connected to the fact that there are gaps before date (t).
			// These gaps take some place on the timeline: see gapsOffset * Metrics.GapHeigh in the final formula.
			int gapsOffset = nextGapIdx;

			// Check if the gap found covers the date (t)
			if (nextGapIdx != gaps.Count && gaps[nextGapIdx].Range.IsInRange(t))
			{
				// If it is we want to stick to the boundaries of the gap. 

				TimeGap next = gaps[nextGapIdx];

				if (t > next.Mid) // Add extra Metrics.GapHeight to Y-coordinate if (t) is over the second half of the range.
				{
					++gapsOffset;
				}

				t = next.Range.Begin; // Stick to the beginning of the range
			}

			// Time span that defines Y-coordinate offset that is proportional to date (t). 
			TimeSpan timeOffset = t - range.Begin;

			// If the gap found is not the first one
			if (nextGapIdx > 0)
			{
				// Get the gap that preceeds the date in question (t)
				TimeGap prev = gaps[nextGapIdx - 1];

				// Substract the time that is covered by time gaps and that we exclude from timeline
				timeOffset -= prev.CumulativeLengthInclusive;
			}

			// The final formula:
			double ret = (double)(
				m.TimeLine.Top + // The origin of Y coordinates
				gapsOffset * Metrics.GapHeight + // The space that is taken by gaps' lines (gaps are shown by fixed height lines)
				timeOffset.TotalMilliseconds * density // The actual offset that depends on (t)
			);

			return ret;
		}

		static int GetYCoordFromDate(Metrics m, DateRange range, double density, DateTime t)
		{
			double ret;

			ret = GetYCoordFromDate_UniformScale(m, range, density, t);

			// Limit the visible to be able to fit to int
			ret = Math.Min(ret, 1e6);
			ret = Math.Max(ret, -1e6);

			return (int)ret;
		}

		DateTime GetDateFromYCoord(Metrics m, int y, out NavigateFlag navFlags)
		{
			DateTime ret = GetDateFromYCoord(m, range, y, out navFlags);
			ret = availableRange.PutInRange(ret);
			if (ret == availableRange.End && ret != DateTime.MinValue)
				ret = availableRange.Maximum;
			return ret;
		}

		DateTime GetDateFromYCoord(Metrics m, int y)
		{
			NavigateFlag navFlags;
			return GetDateFromYCoord(m, y, out navFlags);
		}

		static DateTime GetDateFromYCoord_UniformScale(Metrics m, DateRange range, int y)
		{
			double percent = (double)(y - m.TimeLine.Top) / (double)m.TimeLine.Height;

			TimeSpan toAdd = TimeSpan.FromMilliseconds(percent * range.Length.TotalMilliseconds);

			try
			{
				return range.Begin + toAdd;
			}
			catch (ArgumentOutOfRangeException)
			{
				// There might be overflow
				if (toAdd.Ticks <= 0)
					return range.Begin;
				else
					return range.End;
			}
		}

		static DateTime GetDateFromYCoord_NonUniformScale(Metrics m, DateRange range, ITimeGaps gaps, int y, out NavigateFlag navFlag)
		{
			navFlag = NavigateFlag.AlignCenter | NavigateFlag.OriginDate;

			double density = GetTimeDensity(m, range);

			// Get the closest gap that is after the Y coordinate in quetstion (y)
			int nextGap = gaps.BinarySearch(0, gaps.Count, delegate(TimeGap g)
			{
				return GetYCoordFromDate(m, range, density, g.Range.Begin) <= y;
			});

			// A date that would be an origin of the uniform range nearest to coordinate (y).
			// 'Uniform range' means that we can use *density or /density operations to 
			// convert between dates and coordinates.
			DateTime origin;

			// An offset inside the uniform range
			int dy;

			if (nextGap == 0) // If the gap found is the first one
			{
				// then the origin of the uniform range is the very beginning of the timeline

				origin = range.Begin;
				dy = y - m.TimeLine.Top;
			}
			else
			{
				// otherwise the origin is the end of the previos gap

				DateRange prev = gaps[nextGap - 1].Range; // Get the dates range of the prev. gap
				int tmp = y - GetYCoordFromDate(m, range, density, prev.End); // Get the offset in a temp variable
				if (tmp < 0 && tmp >= -Metrics.GapHeight) // If the offset is inside the area that a gap fills
				{
					// then stick to gap boundaries
					dy = 0;
					if (tmp >= -Metrics.GapHeight / 2)
					{
						origin = prev.End;
						navFlag = NavigateFlag.AlignBottom | NavigateFlag.OriginDate;
					}
					else
					{
						origin = prev.Begin;
						navFlag = NavigateFlag.AlignTop | NavigateFlag.OriginDate;
					}
				}
				else
				{
					// otherwise use gap'factors end as an origin
					dy = tmp;
					origin = prev.End;
				}
			}

			// Get the time span we need to add to the origin
			TimeSpan toAdd = TimeSpan.FromMilliseconds((double)dy / density);

			try
			{
				return origin + toAdd;
			}
			catch (ArgumentOutOfRangeException)
			{
				// There might be overflow
				if (toAdd.Ticks <= 0)
					return range.Begin;
				else
					return range.End;
			}
		}

		static DateTime GetDateFromYCoord(Metrics m, DateRange range, int y)
		{
			NavigateFlag navFlags;
			return GetDateFromYCoord(m, range, y, out navFlags);
		}

		static DateTime GetDateFromYCoord(Metrics m, DateRange range, int y, out NavigateFlag navFlags)
		{
			navFlags = NavigateFlag.AlignCenter | NavigateFlag.OriginDate;
			return GetDateFromYCoord_UniformScale(m, range, y);
		}

		public enum DragArea
		{
			Top, Bottom
		};

		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.Focus();

			if (range.IsEmpty)
				return;

			Metrics m = GetMetrics();

			if (e.Button == MouseButtons.Left)
			{
				if (m.TimeLine.Contains(e.Location))
				{
					NavigateFlag navFlags;
					DateTime d = GetDateFromYCoord(m, e.Y, out navFlags);
					SourcesDrawHelper helper = new SourcesDrawHelper(m, host.SourcesCount);
					var sourceIndex = helper.XCoordToSourceIndex(e.X);
					FireNavigateEvent(d, navFlags, 
						sourceIndex.HasValue ? EnumUtils.NThElement(host.Sources, sourceIndex.Value) : null);
				}
				else if (m.TopDate.Contains(e.Location))
				{
					FireNavigateEvent(range.Begin, 
						availableRange.Begin == range.Begin ?
						(NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries) :
						(NavigateFlag.AlignCenter | NavigateFlag.OriginDate), null);
				}
				else if (m.BottomDate.Contains(e.Location))
				{
					FireNavigateEvent(range.End,
						availableRange.End == range.End ? 
						(NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries) :
						(NavigateFlag.AlignCenter | NavigateFlag.OriginDate), null);
				}
				else if (m.TopDrag.Contains(e.Location) || m.BottomDrag.Contains(e.Location))
				{
					dragPoint = e.Location;
					OnBeginTimeRangeDrag();
				}
			}
			else if (e.Button == MouseButtons.Middle)
			{
				if (m.TimeLine.Contains(e.Location))
				{
					dragPoint = e.Location;
					dragRange = range;
					OnBeginTimeRangeDrag();
				}
			}
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			Point screenPt = Control.MousePosition;
			Point pt = this.PointToClient(screenPt);
			Metrics m = GetMetrics();
			if (m.TopDrag.Contains(pt))
			{
				if (range.Begin != availableRange.Begin)
				{
					StopDragging(false);
					DoSetRangeAnimated(new DateRange(availableRange.Begin, range.End));
					Invalidate();
				}
			}
			else if (m.BottomDrag.Contains(pt))
			{
				if (range.End != availableRange.End)
				{
					StopDragging(false);
					DoSetRangeAnimated(new DateRange(range.Begin, availableRange.End));
					Invalidate();
				}
			}
		}

		HotTrackRange FindHotTrackRange(Metrics m, Point pt)
		{
			HotTrackRange ret = new HotTrackRange();

			SourcesDrawHelper helper = new SourcesDrawHelper(m, host.SourcesCount);

			if (!helper.NeedsDrawing)
				return ret;

			ret.SourceIndex = helper.XCoordToSourceIndex(pt.X);

			if (ret.SourceIndex == null)
			{
				return ret;
			}


			ret.Source = EnumUtils.NThElement(host.Sources, ret.SourceIndex.Value);
			DateRange avaTime = ret.Source.AvailableTime;

			DateTime t = GetDateFromYCoord(m, pt.Y);

			var gaps = ret.Source.TimeGaps.Gaps;

			int gapsBegin = gaps.BinarySearch(0, gaps.Count, delegate(TimeGap g) { return g.Range.End <= avaTime.Begin; });
			int gapsEnd = gaps.BinarySearch(gapsBegin, gaps.Count, delegate(TimeGap g) { return g.Range.Begin < avaTime.End; });
			int gapIdx = gaps.BinarySearch(gapsBegin, gapsEnd, delegate(TimeGap g) { return g.Range.End <= t; });

			if (gapIdx == gapsEnd)
			{
				if (gapsBegin == gapsEnd)
				{
					return ret;
				}
				else
				{
					ret.RangeBegin = gaps[gapIdx - 1];
					DateTime begin = ret.RangeBegin.Value.Range.End;
					ret.Range = new DateRange(begin, avaTime.End);
					return ret;
				}
			}

			TimeGap gap = gaps[gapIdx];

			int y1 = GetYCoordFromDate(m, range, gap.Range.Begin) + StaticMetrics.MinimumTimeSpanHeight / 2;
			int y2 = GetYCoordFromDate(m, range, gap.Range.End) - StaticMetrics.MinimumTimeSpanHeight / 2;

			if (pt.Y <= y1)
			{
				DateTime begin;
				if (gapIdx == 0)
				{
					begin = avaTime.Begin;
					ret.RangeBegin = null;
				}
				else
				{
					ret.RangeBegin = gaps[gapIdx - 1];
					begin = ret.RangeBegin.Value.Range.End;
				}
				ret.Range = new DateRange(begin, gap.Range.Begin);
				ret.RangeEnd = gap;
			}
			else if (pt.Y >= y2)
			{
				DateTime end;
				if (gapIdx == gaps.Count - 1)
				{
					end = avaTime.End;
					ret.RangeEnd = null;
				}
				else
				{
					ret.RangeEnd = gaps[gapIdx + 1];
					end = ret.RangeEnd.Value.Range.Begin;
				}
				ret.Range = new DateRange(gap.Range.End, end);
				ret.RangeBegin = gap;
			}

			return ret;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!this.Capture)
				StopDragging(false);

			if (range.IsEmpty)
			{
				this.Cursor = Cursors.Default;
				return;
			}

			Metrics m = GetMetrics();
			if (dragPoint.HasValue)
			{
				if (m.TimeLine.Contains(dragPoint.Value))
				{
					DoSetRange(dragRange);
					ShiftRange(GetDateFromYCoord(m, dragRange, dragPoint.Value.Y) - GetDateFromYCoord(m, dragRange, e.Y));
				}
				else
				{
					Point mousePt = this.PointToScreen(new Point(dragPoint.Value.X, e.Y));

					if (dragForm == null)
						dragForm = new TimeLineDragForm(this);

					DragArea area = m.TopDrag.Contains(dragPoint.Value) ? DragArea.Top : DragArea.Bottom;
					dragForm.Area = area;

					DateTime d = GetDateFromYCoord(
						m,
						e.Y - dragPoint.Value.Y +
							(area == DragArea.Top ? m.TimeLine.Top : m.TimeLine.Bottom)
					);
					d = availableRange.PutInRange(d);
					dragForm.Date = d;

					Point pt1 = this.PointToScreen(new Point());
					Point pt2 = this.PointToScreen(new Point(ClientSize.Width, 0));
					int formHeight = datesSize.Height + DragAreaHeight;
					dragForm.SetBounds(
						pt1.X,
						pt1.Y + (int)GetYCoordFromDate(m, range, d) +
							(area == DragArea.Top ? -formHeight : 0),
						pt2.X - pt1.X,
						formHeight
					);

					CreateNewStatusReport().ShowStatusText(
						string.Format("Changing the {0} bound of the time line to {1}. ESC to cancel.",
						area == DragArea.Top ? "upper" : "lower",
							GetUserFriendlyFullDateTimeString(d)), false);

					if (!dragForm.Visible)
					{
						dragForm.Visible = true;
						this.Focus();
					}
				}
			}
			else
			{
				if (m.TopDrag.Contains(e.Location) || m.BottomDrag.Contains(e.Location))
				{
					this.Cursor = Cursors.SizeNS;
					if (hotTrackDate.HasValue)
					{
						hotTrackDate = new DateTime?();
						Invalidate();
					}

					string txt;
					if (m.TopDrag.Contains(e.Location))
					{
						txt = "Click and drag to change the lower bound of the time line.";
						if (range.Begin != availableRange.Begin)
							txt += " Double-click to restore the initial value.";
					}
					else
					{
						txt = "Click and drag to change the upper bound of the time line.";
						if (range.End != availableRange.End)
							txt += " Double-click to restore the initial value.";
					}

					CreateNewStatusReport().ShowStatusText(txt, false);
				}
				else
				{
					this.Cursor = host.IsBusy ? Cursors.WaitCursor : Cursors.Default;
					UpdateHotTrackDate(m, e.Y);
					if (lastToolTipPoint == null
					 || (Math.Abs(lastToolTipPoint.Value.X - e.X) + Math.Abs(lastToolTipPoint.Value.Y - e.Y)) > 4)
					{
						OnResetToolTip();
					}
					Invalidate();
				}
			}
		}

		void UpdateHotTrackDate(Metrics m, int y)
		{
			hotTrackDate = range.PutInRange(GetDateFromYCoord(m, y));
			string msg = "";
			if (range.End == availableRange.End && m.BottomDate.Contains(m.BottomDate.Left, y))
				msg = string.Format("Click to stick to the end of the log");
			else
				msg = string.Format("Click to see what was happening at around {0}.{1}",
						GetUserFriendlyFullDateTimeString(hotTrackDate.Value),
						Focused ? " Ctrl + Mouse Wheel to zoom timeline." : "");
			CreateNewStatusReport().ShowStatusText(msg, false);
		}

		protected virtual void OnBeginTimeRangeDrag()
		{
			presenter.OnBeginTimeRangeDrag();
		}

		protected virtual void OnEndTimeRangeDrag()
		{
			presenter.OnEndTimeRangeDrag();
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (range.IsEmpty) // todo: create IsEmpty method to check if the control is empty
				return;

			Metrics m = GetMetrics();
			if (Control.ModifierKeys == Keys.Control)
			{
				ZoomRange(GetDateFromYCoord(m, e.Location.Y), -e.Delta);
			}
			else
			{
				ShiftRange(-e.Delta);
				UpdateHotTrackDate(m, e.Location.Y);
			}
		}

		void ZoomRange(DateTime zoomAt, int delta)
		{
			if (delta == 0)
				return;

			long scale = 100 + Math.Sign(delta) * 20;

			DateRange tmp = new DateRange(
				zoomAt - new TimeSpan((zoomAt - range.Begin).Ticks * scale / 100),
				zoomAt + new TimeSpan((range.End - zoomAt).Ticks * scale / 100)
			);

			DoSetRange(DateRange.Intersect(availableRange, tmp));

			Invalidate();
		}

		void ShiftRange(int delta)
		{
			if (delta == 0)
				return;

			ShiftRange(new TimeSpan(Math.Sign(delta) * range.Length.Ticks / 20));
		}

		void ShiftRange(TimeSpan offset)
		{
			long offsetTicks = offset.Ticks;

			offsetTicks = Math.Max(offsetTicks, (availableRange.Begin - range.Begin).Ticks);
			offsetTicks = Math.Min(offsetTicks, (availableRange.End - range.End).Ticks);

			offset = new TimeSpan(offsetTicks);

			DoSetRange(new DateRange(range.Begin + offset, range.End + offset));

			Invalidate();
		}


		void DoSetRangeAnimated(DateRange newRange)
		{
			if (newRange.IsEmpty || newRange.Begin == newRange.Maximum)
				return;
			if (range.Equals(newRange))
				return;

			int stepsCount = 8;
			TimeSpan deltaB = new TimeSpan((newRange.Begin - range.Begin).Ticks / stepsCount);
			TimeSpan deltaE = new TimeSpan((newRange.End - range.End).Ticks / stepsCount);
			bool animateDragForm = dragForm != null && dragForm.Visible;
			Metrics m = GetMetrics();

			DateRange i = range;
			for (int step = 0; step < stepsCount; ++step)
			{
				animationRange = i;
				if (animateDragForm)
				{
					if (deltaB.Ticks != 0)
					{
						int y = GetYCoordFromDate(m, animationRange.Value, newRange.Begin);
						dragForm.Top = this.PointToScreen(new Point(0, y)).Y - dragForm.Height;
					}
					else if (deltaE.Ticks != 0)
					{
						int y = GetYCoordFromDate(m, animationRange.Value, newRange.End);
						dragForm.Top = this.PointToScreen(new Point(0, y)).Y;
					}
				}

				this.Refresh();
				System.Threading.Thread.Sleep(20);

				i = new DateRange(i.Begin + deltaB, i.End + deltaE);
			}
			animationRange = new DateRange?();

			DoSetRange(newRange);
		}


		void StopDragging(bool accept)
		{
			if (dragPoint.HasValue)
			{
				if (accept && dragForm != null && dragForm.Visible)
				{
					if (dragForm.Area == DragArea.Top)
					{
						DoSetRangeAnimated(new DateRange(dragForm.Date, range.End));
					}
					else
					{
						DoSetRangeAnimated(new DateRange(range.Begin, dragForm.Date));
					}
					Invalidate();
				}
				dragPoint = new Point?();
				OnEndTimeRangeDrag();
			}
			if (dragForm != null && dragForm.Visible)
			{
				dragForm.Visible = false;
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			StopDragging(true);
			base.OnMouseUp(e);
		}

		protected override void OnMouseCaptureChanged(EventArgs e)
		{
			StopDragging(false);
			base.OnMouseCaptureChanged(e);
		}

		void FireNavigateEvent(DateTime val, NavigateFlag flags, ILogSource source)
		{
			if (range.IsEmpty)
				return;
			DateTime newVal = range.PutInRange(val);
			if (newVal == range.End)
				newVal = range.Maximum;
			presenter.OnNavigate(new TimeNavigateEventArgs(newVal, flags, source));
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Escape)
				return false;
			return base.IsInputKey(keyData);
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			return base.ProcessDialogKey(keyData);
		}

		protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				if (dragPoint.HasValue)
				{
					StopDragging(false);
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Apps)
			{
			}
			base.OnKeyDown(e);
		}

		void RelaseStatusReport()
		{
			if (statusReport != null)
			{
				statusReport.Dispose();
				statusReport = null;
			}
		}

		void ReleaseHotTrack()
		{
			if (hotTrackDate.HasValue)
			{
				hotTrackDate = new DateTime?();
				Invalidate();
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			RelaseStatusReport();
			ReleaseHotTrack();
			base.OnMouseLeave(e);
		}

		struct Metrics
		{
			public Rectangle Client;
			public Rectangle TopDrag;
			public Rectangle TopDate;
			public Rectangle TimeLine;
			public Rectangle BottomDate;
			public Rectangle BottomDrag;

			public const int GapHeight = 5;
		};

		Metrics GetMetrics()
		{
			Metrics r;
			r.Client = this.ClientRectangle;
			r.TopDrag = new Rectangle(DragAreaHeight / 2, 0, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.TopDate = new Rectangle(0, r.TopDrag.Bottom, r.Client.Width, datesSize.Height);
			r.BottomDrag = new Rectangle(DragAreaHeight / 2, r.Client.Height - DragAreaHeight, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.BottomDate = new Rectangle(0, r.BottomDrag.Top - datesSize.Height, r.Client.Width, datesSize.Height);
			r.TimeLine = new Rectangle(0, r.TopDate.Bottom, r.Client.Width, 
				r.BottomDate.Top - r.TopDate.Bottom - StaticMetrics.SourceShadowSize.Height - StaticMetrics.SourcesBottomPadding);
			return r;
		}

		Presenters.StatusReports.IReport CreateNewStatusReport()
		{
			if (statusReport == null)
				statusReport = host.CreateNewStatusReport();
			return statusReport;
		}

		RulerIntervals? FindRulerIntervals()
		{
			return FindRulerIntervals(GetMetrics(), range.Length.Ticks);
		}

		RulerIntervals? FindRulerIntervals(Metrics m, long totalTicks)
		{
			if (totalTicks <= 0)
				return null;
			int minMarkHeight = 25;
			if (m.TimeLine.Height <= minMarkHeight)
				return null;
			return presenter.FindRulerIntervals(
				new TimeSpan(NumUtils.MulDiv(totalTicks, minMarkHeight, m.TimeLine.Height)));
		}

		public string GetUserFriendlyFullDateTimeString(DateTime d)
		{
			return GetUserFriendlyFullDateTimeString(d, FindRulerIntervals());
		}

		string GetUserFriendlyFullDateTimeString(DateTime d, RulerIntervals? ri, bool showDate = true)
		{
			return (new MessageTimestamp(d)).ToUserFrendlyString(AreMillisecondsVisibleInternal(ri), showDate);
		}

		static bool AreMillisecondsVisibleInternal(RulerIntervals? ri)
		{
			return ri.HasValue && ri.Value.Minor.Component == DateComponent.Milliseconds;
		}
		
		bool DoSetRange(DateRange r)
		{
			if (r.Equals(range))
				return false;
			range = r;
			presenter.OnRangeChanged();
			return true;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (range.IsEmpty)
			{
				e.Cancel = true;
				return;
			}
			
			resetTimeLineMenuItem.Enabled = !availableRange.Equals(range);
			viewTailModeMenuItem.Checked = host.IsInViewTailMode;

			HotTrackRange tmp = FindHotTrackRange(GetMetrics(), PointToClient(Control.MousePosition));
			zoomToMenuItem.Text = "";
			string zoomToMenuItemFormat = null;
			DateRange zoomToRange = new DateRange();
			if (tmp.Range != null)
			{
				zoomToMenuItemFormat = "Zoom to this time period ({0} - {1})";
				zoomToRange = tmp.Range.Value;
			}
			else if (tmp.Source != null)
			{
				if (host.SourcesCount > 1)
				{
					zoomToMenuItemFormat = "Zoom to this log source ({0} - {1})";
					zoomToRange = tmp.Source.AvailableTime;
				}
			}
			if (zoomToRange.IsEmpty)
			{
				zoomToMenuItemFormat = null;
			}

			zoomToMenuItem.Visible = zoomToMenuItemFormat != null;
			if (zoomToMenuItemFormat != null)
			{
				RulerIntervals? ri = FindRulerIntervals();
				DateRange r = zoomToRange;
				zoomToMenuItem.Text = string.Format(zoomToMenuItemFormat,
					GetUserFriendlyFullDateTimeString(r.Begin, ri), 
					GetUserFriendlyFullDateTimeString(r.Maximum, ri));
				zoomToMenuItem.Tag = zoomToRange;
			}

			SetHotTrackRange(tmp);
		}

		private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (e.ClickedItem == resetTimeLineMenuItem)
			{
				DoSetRangeAnimated(availableRange);
			}
			else if (e.ClickedItem == viewTailModeMenuItem)
			{
				if (!viewTailModeMenuItem.Checked)
					TrySwitchOnViewTailMode();
				else
					TrySwitchOffViewTailMode();
			}
			else if (e.ClickedItem == zoomToMenuItem)
			{
				DoSetRangeAnimated((DateRange)zoomToMenuItem.Tag);
			}
		}

		bool toolTipVisible = false;

		void HideToolTip()
		{
			if (!toolTipVisible)
				return;
			toolTip.Hide(this);
			toolTipVisible = false;
			lastToolTipPoint = null;
		}

		void ShowToolTip()
		{
			if (toolTipVisible)
				return;
			if (this.contextMenu.Visible)
				return;
			Point pt = Cursor.Position;
			Point clientPt = PointToClient(pt);
			if (!ClientRectangle.Contains(clientPt))
				return;
			HotTrackRange range = FindHotTrackRange(GetMetrics(), clientPt);
			if (range.Source == null)
				return;
			lastToolTipPoint = clientPt;
			Cursor cursor = this.Cursor;
			if (cursor != null)
			{
				pt.Y += cursor.Size.Height - cursor.HotSpot.Y;
			}
			toolTip.Show(range.ToString(), this, PointToClient(pt));
			toolTipVisible = true;
		}

		void OnResetToolTip()
		{
			HideToolTip();
			toolTipTimer.Stop();
			toolTipTimer.Start();
		}

		void SetHotTrackRange(HotTrackRange range)
		{
			hotTrackRange = range;
			Invalidate();
		}

		private void toolTipTimer_Tick(object sender, EventArgs e)
		{
			ShowToolTip();
			toolTipTimer.Stop();
		}

		private void contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			SetHotTrackRange(new HotTrackRange());
		}

		public const int DragAreaHeight = 5;

		DateRange availableRange;
		DateRange range;
		DateRange? animationRange;
		Size datesSize;
		Presenters.StatusReports.IReport statusReport;
		DateTime? hotTrackDate;
		Point? dragPoint;
		DateRange dragRange;
		TimeLineDragForm dragForm;
		Point? lastToolTipPoint;
		static readonly GraphicsPath roundRectsPath = new GraphicsPath();
		struct HotTrackRange
		{
			public ILogSource Source;
			public int? SourceIndex;
			public DateRange? Range;
			public TimeGap? RangeBegin;
			public TimeGap? RangeEnd;

			public bool Equals(HotTrackRange r)
			{
				if (SourceIndex.GetValueOrDefault(-1) != r.SourceIndex.GetValueOrDefault(-1))
					return false;
				if (!Range.GetValueOrDefault(new DateRange()).Equals(r.Range.GetValueOrDefault(new DateRange())))
					return false;
				return true;
			}

			public override string ToString()
			{
				StringBuilder ret = new StringBuilder();
				if (Source != null)
				{
					ret.Append(Source.DisplayName);
					if (Range != null)
					{
						ret.AppendLine();
						ret.AppendFormat("Time period: {0} - {1}", Range.Value.Begin, Range.Value.Maximum);
					}
				}
				return ret.ToString();
			}
		};
		HotTrackRange hotTrackRange;
		Resources res = new Resources();
		IViewEvents presenter;
		IViewEvents host { get { return presenter; } }
		readonly UIUtils.FocuslessMouseWheelMessagingFilter focuslessMouseWheelMessagingFilter;
	}
}
