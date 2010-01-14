using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class TimeLineControl : Control
	{
		public TimeLineControl()
		{
			InitializeComponent();
			
			this.SetStyle(ControlStyles.ResizeRedraw, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		}

		public event EventHandler<TimeNavigateEventArgs> Navigate;
		public event EventHandler<EventArgs> RangeChanged;

		public event EventHandler BeginTimeRangeDrag;
		public event EventHandler EndTimeRangeDrag;

		public static void DrawDragEllipsis(Graphics g, Rectangle r)
		{
			int y = r.Top + 1;
			for (int i = r.Left; i < r.Right; i += 5)
			{
				g.FillRectangle(Brushes.White, i+1, y+1, 2, 2);
				g.FillRectangle(Brushes.DarkGray, i, y, 2, 2);
			}
		}

		static void AddRoundRect(GraphicsPath gp, Rectangle rect, int radius)
		{
			int diameter = radius * 2; 
			Size size = new Size(diameter, diameter);
			Rectangle arc = new Rectangle(rect.Location, size); 

			gp.AddArc(arc, 180, 90);

			arc.X = rect.Right-diameter;
			gp.AddArc(arc, 270, 90);

			arc.Y = rect.Bottom-diameter;
			gp.AddArc(arc, 0, 90);

			arc.X = rect.Left;
			gp.AddArc(arc, 90, 90);

			gp.CloseFigure();
		}

		static void DrawTimeLineRange(Graphics g, int y1, int y2, int x1, int width, Brush brush)
		{
			int minRangeDispayHeight = 4;
			if (y2 - y1 < minRangeDispayHeight)
			{
				y1 -= minRangeDispayHeight / 2;
				y2 += minRangeDispayHeight / 2;
			}

			int radius = 5;

			if (y2 - y1 < radius * 2
			 || width < radius * 2)
			{
				g.FillRectangle(brush, x1, y1, width, y2 - y1);
				g.DrawRectangle(Pens.Gray, x1, y1, width, y2 - y1);
			}
			else
			{
				GraphicsPath gp = roundRectsPath;
				gp.Reset();
				AddRoundRect(gp, new Rectangle(x1, y1, width, y2 - y1), radius);
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.FillPath(brush, gp);
				g.DrawPath(Pens.Gray, gp);
				g.SmoothingMode = SmoothingMode.HighSpeed;
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			Graphics g = pe.Graphics;
			g.FillRectangle(SystemBrushes.Window, ClientRectangle);

			DateRange drange = animationRange.GetValueOrDefault(range);

			if (drange.IsEmpty)
				return;

			int sourcesCount = host.SourcesCount;

			if (sourcesCount == 0)
				return;

			Metrics m = GetMetrics();

			TimeSpan total = drange.Length;

			Rectangle r = m.TimeLine;
			int sourceDx = r.Width / sourcesCount;
			if (sourceDx > 2)
			{
				int x = 1;
				foreach (ITimeLineSource src in host.Sources)
				{
					DateRange avaTime = src.AvailableTime;
					int y1 = GetYCoordFromDate(m, drange, avaTime.Begin);
					int y2 = GetYCoordFromDate(m, drange, avaTime.End);
					DateRange loadedTime = src.LoadedTime;
					int y3 = GetYCoordFromDate(m, drange, loadedTime.Begin);
					int y4 = GetYCoordFromDate(m, drange, loadedTime.End);
					using (SolidBrush sb = new SolidBrush(src.Color))
					{
						DrawTimeLineRange(g, y1, y2-1, x, sourceDx - 2, sb);
					}
					using (SolidBrush sb = new SolidBrush(PastelColorsGenerator.MakeDarker(src.Color)))
					{
						DrawTimeLineRange(g, y3, y4-1, x, sourceDx - 2, sb);
					}

					x += sourceDx;
				}
			}

			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			RulerIntervals? rulerIntervals = FindRulerIntervals(m, total.Ticks);
			if (rulerIntervals.HasValue)
			{
				using (Pen p = new Pen(Color.Gray, 1))
				using (Pen p2 = new Pen(Color.Gray, 1))
				using (Font font = new Font(this.Font.Name, 6))
				using (StringFormat sf = new StringFormat())
				{
					sf.Alignment = StringAlignment.Far;

					p.DashPattern = new float[] { 1, 3 };
					p2.DashPattern = new float[] { 4, 1 };
					foreach (RulerMark rm in GenerateRulerMarks(rulerIntervals.Value, drange))
					{
						int y = GetYCoordFromDate(m, drange, rm.Time);
						g.DrawLine(rm.IsMajor ? p2 : p, 0, y, m.Client.Width, y);
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
								labelFmt = "ffff";
								break;
						}
						if (labelFmt != null)
						{
							string label = rm.Time.ToString(labelFmt);
							g.DrawString(label, font, Brushes.Gray, 3, y);
						}
					}
				}
			}

			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;

			using (StringFormat fmt = new StringFormat())
			{
				fmt.Alignment = StringAlignment.Center;
				int center = (m.Client.Left + m.Client.Right) / 2;

				g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
					0, m.TopDrag.Y, m.Client.Width, m.TopDate.Bottom - m.TopDrag.Y
				));
				g.DrawString(
					GetUserFriendlyFullDateTimeString(range.Begin, rulerIntervals),
					this.Font, Brushes.Black, center, m.TopDate.Top, fmt);
				DrawDragEllipsis(g, m.TopDrag);

				g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
					0, m.BottomDate.Y, m.Client.Width, m.BottomDrag.Bottom - m.BottomDate.Y
				));
				g.DrawString(GetUserFriendlyFullDateTimeString(range.End, rulerIntervals),
					this.Font, Brushes.Black, center, m.BottomDate.Top, fmt);
				DrawDragEllipsis(g, m.BottomDrag);
			}

			foreach (IBookmark bmk in host.Bookmarks)
			{
				if (!drange.IsInRange(bmk.Time))
					continue;
				int y = GetYCoordFromDate(m, drange, bmk.Time);
				bool hidden = false;
				if (bmk.Thread != null)
					if (!bmk.Thread.ThreadMessagesAreVisible)
						hidden = true;
				using (Pen bookmarkPen = new Pen(Color.FromArgb(0x5b, 0x87, 0xe0)))
				{
					if (hidden)
						bookmarkPen.DashPattern = new float[] { 10, 3 };
					g.DrawLine(bookmarkPen, m.Client.Left, y, m.Client.Right, y);
				}
				Image img = this.bookmarkPictureBox.Image;
				g.DrawImage(img,
					m.Client.Right - img.Width - 2, 
					y - 2,
					img.Width,
					img.Height
				);
			}

			foreach (TimeGap gap in host.TimeGaps)
			{
				int y1 = GetYCoordFromDate(m, drange, gap.Range.Begin);
				int y2 = GetYCoordFromDate(m, drange, gap.Range.End);
				using (Pen p = new Pen(Color.Red))
				{
					g.DrawRectangle(p, m.Client.Left, y1, m.Client.Width, y2 - y1);
				}
			}

			DateTime? curr = host.CurrentViewTime;
			if (curr.HasValue && drange.IsInRange(curr.Value))
			{
				int y = GetYCoordFromDate(m, drange, curr.Value);
				g.DrawLine(Pens.Blue, m.Client.Left, y, m.Client.Right, y);
			}

			if (hotTrackDate.HasValue)
			{
				GraphicsPath hotDateMarker = new GraphicsPath();
				hotDateMarker.AddPolygon(new Point[] {
					new Point(4, 0),
					new Point(0, 4),
					new Point(0, -4)
				});
				using (Pen p = new Pen(Color.FromArgb(128, Color.Red), 1))
				{
					int y = GetYCoordFromDate(m, drange, hotTrackDate.Value);
					GraphicsState s = g.Save();
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.TranslateTransform(0, y);
					g.DrawLine(p, 0, 0, m.TimeLine.Right, 0);
					g.FillPath(Brushes.Red, hotDateMarker);
					g.TranslateTransform(m.TimeLine.Width - 1, 0);
					g.ScaleTransform(-1, 1, MatrixOrder.Prepend);
					g.FillPath(Brushes.Red, hotDateMarker);
					g.Restore(s);
				}
			}

			if (Focused)
			{
				if (host.FocusRectIsRequired)
				{
					ControlPaint.DrawFocusRectangle(g, this.ClientRectangle);
				}
			}

			base.OnPaint(pe);
		}

		public void SetHost(ITimeLineControlHost host)
		{
			this.host = host;
			InternalUpdate();
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

		public void UpdateView()
		{
			InternalUpdate();
		}

		void UpdateRange()
		{
			DateRange union = DateRange.MakeEmpty();
			foreach (ITimeLineSource s in host.Sources)
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

		public DateRange TimeRange
		{
			get { return range; }
		}

		public DateRange AvailableTimeRange
		{
			get { return availableRange; }
		}

		static int GetYCoordFromDate(Metrics m, DateRange range, DateTime t)
		{
			return GetYCoordFromDate(m, range, t, false);
		}

		static int GetYCoordFromDate(Metrics m, DateRange range, DateTime t, bool checkRange)
		{
			if (checkRange)
				t = range.PutInRange(t);
			TimeSpan pos = t - range.Begin;
			double ret = (double)(m.TimeLine.Top + m.TimeLine.Height * (pos.TotalMilliseconds / range.Length.TotalMilliseconds));
			ret = Math.Min(ret, 1e6);
			ret = Math.Max(ret, -1e6);
			return (int)ret;
		}

		DateTime GetDateFromYCoord(Metrics m, int y)
		{
			DateTime ret = GetDateFromYCoord(m, range, y);
			ret = availableRange.PutInRange(ret);
			if (ret == availableRange.End && ret != DateTime.MinValue)
				ret = availableRange.UpperBound;
			return ret;
		}

		static DateTime GetDateFromYCoord(Metrics m, DateRange range, int y)
		{
			double percent = (double)(y - m.TimeLine.Top) / (double)m.TimeLine.Height;
			TimeSpan toAdd = TimeSpan.FromMilliseconds(percent * range.Length.TotalMilliseconds);
			try
			{
				return range.Begin + toAdd;
			}
			catch (ArgumentOutOfRangeException)
			{
				if (toAdd.Ticks <= 0)
					return range.Begin;
				else
					return range.End;
			}
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
					FireNavigateEvent(GetDateFromYCoord(m, e.Y), NavigateFlags.None);
				}
				else if (m.TopDate.Contains(e.Location))
				{
					FireNavigateEvent(range.Begin, availableRange.Begin == range.Begin ? NavigateFlags.Top : NavigateFlags.None);
				}
				else if (m.BottomDate.Contains(e.Location))
				{
					FireNavigateEvent(range.End, availableRange.End == range.End ? NavigateFlags.Bottom : NavigateFlags.None);
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
						pt1.Y + (int)GetYCoordFromDate(m, range, d, false) +
							(area == DragArea.Top ? -formHeight : 0),
						pt2.X - pt1.X,
						formHeight
					);

					GetStatusReport().SetStatusString(
						string.Format("Changing the {0} bound of the time line to {1}. ESC to cancel.",
						area == DragArea.Top ? "upper" : "lower",
							GetUserFriendlyFullDateTimeString(d)));

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

					GetStatusReport().SetStatusString(txt);
				}
				else
				{
					this.Cursor = Cursors.Default;
					UpdateHotTrackDate(m, e.Y);
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
						Focused ? " Ctrl + Mouse Wheel to zoom view." : "");
			GetStatusReport().SetStatusString(msg);
		}

		protected virtual void OnBeginTimeRangeDrag()
		{
			if (BeginTimeRangeDrag != null)
				BeginTimeRangeDrag(this, EventArgs.Empty);
		}

		protected virtual void OnEndTimeRangeDrag()
		{
			if (EndTimeRangeDrag != null)
				EndTimeRangeDrag(this, EventArgs.Empty);
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
			if (newRange.IsEmpty || newRange.Begin == newRange.UpperBound)
				return;
			if (range.Equals(newRange))
				return;

			TimeSpan deltaB = new TimeSpan((newRange.Begin - range.Begin).Ticks / 8);
			TimeSpan deltaE = new TimeSpan((newRange.End - range.End).Ticks / 8);
			bool animateDragForm = dragForm != null && dragForm.Visible;
			Metrics m = GetMetrics();

			for (DateRange i = range; ; )
			{
				bool continueFlag = false;
				if (deltaB.Ticks != 0)
					continueFlag |= deltaB.Ticks > 0 ? i.Begin <= newRange.Begin : i.Begin >= newRange.Begin;
				if (deltaE.Ticks != 0)
					continueFlag |= deltaE.Ticks > 0 ? i.End <= newRange.End : i.End >= newRange.End;
				if (!continueFlag)
					break;

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

		void FireNavigateEvent(DateTime val, NavigateFlags flags)
		{
			DateTime newVal = range.PutInRange(val);
			if (newVal == range.End)
				newVal = range.UpperBound;
			if (Navigate != null)
				Navigate(this, new TimeNavigateEventArgs(newVal, flags));
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
		};

		Metrics GetMetrics()
		{
			Metrics r;
			r.Client = this.ClientRectangle;
			r.TopDrag = new Rectangle(DragAreaHeight / 2, 0, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.TopDate = new Rectangle(0, r.TopDrag.Bottom, r.Client.Width, datesSize.Height);
			r.BottomDrag = new Rectangle(DragAreaHeight / 2, r.Client.Height - DragAreaHeight, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.BottomDate = new Rectangle(0, r.BottomDrag.Top - datesSize.Height, r.Client.Width, datesSize.Height);
			r.TimeLine = new Rectangle(0, r.TopDate.Bottom, r.Client.Width, r.BottomDate.Top - r.TopDate.Bottom);
			return r;
		}

		IStatusReport GetStatusReport()
		{
			if (statusReport == null)
				statusReport = host.GetStatusReport();
			return statusReport;
		}

		enum DateComponent
		{
			None,
			Year,
			Month,
			Day,
			Hour,
			Minute,
			Seconds,
			Milliseconds
		};

		struct RulerInterval
		{
			public readonly TimeSpan Duration;
			public readonly DateComponent Component;
			public readonly int NonUniformComponentCount;
			public bool IsHiddenWhenMajor;

			public RulerInterval(TimeSpan dur, int nonUniformComonentCount, DateComponent comp)
			{
				Duration = dur;
				Component = comp;
				NonUniformComponentCount = nonUniformComonentCount;
				IsHiddenWhenMajor = false;
			}

			public RulerInterval MakeHiddenWhenMajor()
			{
				IsHiddenWhenMajor = true;
				return this;
			}

			public static RulerInterval FromYears(int years)
			{
				return new RulerInterval(DateTime.MinValue.AddYears(years) - DateTime.MinValue, years, DateComponent.Year);
			}
			public static RulerInterval FromMonths(int months)
			{
				return new RulerInterval(DateTime.MinValue.AddMonths(months) - DateTime.MinValue, months, DateComponent.Month);
			}
			public static RulerInterval FromDays(int days)
			{
				return new RulerInterval(TimeSpan.FromDays(days), 0, DateComponent.Day);
			}
			public static RulerInterval FromHours(int hours)
			{
				return new RulerInterval(TimeSpan.FromHours(hours), 0, DateComponent.Hour);
			}
			public static RulerInterval FromMinutes(int minutes)
			{
				return new RulerInterval(TimeSpan.FromMinutes(minutes), 0, DateComponent.Minute);
			}
			public static RulerInterval FromSeconds(double seconds)
			{
				return new RulerInterval(TimeSpan.FromSeconds(seconds), 0, DateComponent.Seconds);
			}
			public static RulerInterval FromMilliseconds(double mseconds)
			{
				return new RulerInterval(TimeSpan.FromMilliseconds(mseconds), 0, DateComponent.Milliseconds);
			}

			public DateTime StickToIntervalBounds(DateTime d)
			{
				if (Component == DateComponent.Year)
				{
					int year = (d.Year / NonUniformComponentCount) * NonUniformComponentCount;
					if (year == 0)
						return d;
					return new DateTime(year, 1, 1);
				}

				if (Component == DateComponent.Month)
					return new DateTime(d.Year, ((d.Month - 1) / NonUniformComponentCount) * NonUniformComponentCount + 1, 1);

				long durTicks = Duration.Ticks;

				if (durTicks == 0)
					return d;

				return new DateTime((d.Ticks / Duration.Ticks) * Duration.Ticks);
			}

			public DateTime MoveDate(DateTime d)
			{
				if (Component == DateComponent.Year)
					return d.AddYears(NonUniformComponentCount);
				if (Component == DateComponent.Month)
					return d.AddMonths(NonUniformComponentCount);
				return d.Add(Duration);
			}

		};

		static readonly RulerInterval[] predefinedRulerIntervals = new RulerInterval[]
		{
			RulerInterval.FromYears(1000),
			RulerInterval.FromYears(100),
			RulerInterval.FromYears(25),
			RulerInterval.FromYears(5),
			RulerInterval.FromYears(1),
			RulerInterval.FromMonths(3),
			RulerInterval.FromMonths(1).MakeHiddenWhenMajor(),
			RulerInterval.FromDays(7).MakeHiddenWhenMajor(),
			RulerInterval.FromDays(3),
			RulerInterval.FromDays(1),
			RulerInterval.FromHours(6),
			RulerInterval.FromHours(1),
			RulerInterval.FromMinutes(20),
			RulerInterval.FromMinutes(5),
			RulerInterval.FromMinutes(1),
			RulerInterval.FromSeconds(20),
			RulerInterval.FromSeconds(5),
			RulerInterval.FromSeconds(1),
			RulerInterval.FromMilliseconds(200),
			RulerInterval.FromMilliseconds(50),
			RulerInterval.FromMilliseconds(10),
			RulerInterval.FromMilliseconds(2),
			RulerInterval.FromMilliseconds(1)
		};

		struct RulerIntervals
		{
			public readonly RulerInterval Major, Minor;
			public RulerIntervals(RulerInterval major, RulerInterval minor)
			{
				Major = major;
				Minor = minor;
			}
		};

		struct RulerMark
		{
			public readonly DateTime Time;
			public readonly bool IsMajor;
			public readonly DateComponent Component;

			public RulerMark(DateTime d, bool isMajor, DateComponent comp)
			{
				Time = d;
				IsMajor = isMajor;
				Component = comp;
			}
		};

		RulerIntervals? FindRulerIntervals()
		{
			return FindRulerIntervals(GetMetrics(), range.Length.Ticks);
		}

		static long MulDiv(long a, int b, int c)
		{
			long whole = (a / c) * b;
			long fraction = (a % c) * b / c;
			return whole + fraction;
		}

		RulerIntervals? FindRulerIntervals(Metrics m, long totalTicks)
		{
			int minMarkHeight = 25;
			return FindRulerIntervals(
				new TimeSpan(MulDiv(totalTicks, minMarkHeight, m.TimeLine.Height)));
		}

		static RulerIntervals? FindRulerIntervals(TimeSpan minSpan)
		{
			for (int i = predefinedRulerIntervals.Length - 1; i >= 0; --i)
			{
				if (predefinedRulerIntervals[i].Duration > minSpan)
				{
					if (i == 0)
						i = 1;
					return new RulerIntervals(predefinedRulerIntervals[i - 1], predefinedRulerIntervals[i]);
				}
			}
			return null;
		}

		static IEnumerable<RulerMark> GenerateRulerMarks(RulerIntervals intervals, DateRange range)
		{
			RulerInterval major = intervals.Major;
			RulerInterval minor = intervals.Minor;

			DateTime lastMajor = DateTime.MaxValue;
			for (DateTime d = major.StickToIntervalBounds(range.Begin); 
				d < range.End; d = minor.MoveDate(d))
			{
				if (d < range.Begin)
					continue;
				if (!major.IsHiddenWhenMajor)
				{
					DateTime tmp = major.StickToIntervalBounds(d);
					if (tmp >= range.Begin && tmp != lastMajor)
					{
						yield return new RulerMark(tmp, true, major.Component);
						lastMajor = tmp;
						if (tmp == d)
							continue;
					}
					yield return new RulerMark(d, false, minor.Component);
				}
				else
				{
					yield return new RulerMark(d, true, minor.Component);
				}
			}
		}

		public bool AreMillisecondsVisible
		{
			get			
			{
				return AreMillisecondsVisibleInternal(FindRulerIntervals());
			}
		}

		public string GetUserFriendlyFullDateTimeString(DateTime d)
		{
			return GetUserFriendlyFullDateTimeString(d, FindRulerIntervals());
		}

		string GetUserFriendlyFullDateTimeString(DateTime d, RulerIntervals? ri)
		{
			return MessageBase.FormatTime(d, AreMillisecondsVisibleInternal(ri));
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
			if (RangeChanged != null)
				RangeChanged(this, EventArgs.Empty);
			return true;
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (range.IsEmpty)
			{
				e.Cancel = true;
				return;
			}
			
			resetTimeLineMenuItem.Visible = !availableRange.Equals(range);
			viewTailModeMenuItem.Checked = host.IsInViewTailMode;
		}

		private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (e.ClickedItem == resetTimeLineMenuItem)
			{
				DoSetRangeAnimated(availableRange);
			}
			else if (e.ClickedItem == viewTailModeMenuItem)
			{
				FireNavigateEvent(availableRange.End, 
					!viewTailModeMenuItem.Checked ? NavigateFlags.Bottom : NavigateFlags.None);
			}
		}

		public const int DragAreaHeight = 5;

		ITimeLineControlHost host;
		DateRange availableRange;
		DateRange range;
		DateRange? animationRange;
		Size datesSize;
		IStatusReport statusReport;
		DateTime? hotTrackDate;
		Point? dragPoint;
		DateRange dragRange;
		TimeLineDragForm dragForm;
		static readonly GraphicsPath roundRectsPath = new GraphicsPath();

	}

	public interface ITimeLineSource
	{
		DateRange AvailableTime { get; }
		DateRange LoadedTime { get; }
		Color Color { get; }
	};

	public interface ITimeLineControlHost
	{
		IEnumerable<ITimeLineSource> Sources { get; }
		int SourcesCount { get; }
		DateTime? CurrentViewTime { get; }
		IStatusReport GetStatusReport();
		IEnumerable<IBookmark> Bookmarks { get; }
		bool FocusRectIsRequired { get; }
		bool IsInViewTailMode { get; }
		IList<TimeGap> TimeGaps { get; }
	};

	public enum NavigateFlags
	{
		None,
		Top,
		Bottom
	};

	public class TimeNavigateEventArgs : EventArgs
	{
		public TimeNavigateEventArgs(DateTime date, NavigateFlags flags)
		{
			this.date = date;
			this.flags = flags;
		}
		public DateTime Date { get { return date; } }
		public NavigateFlags Flags { get { return flags; } }

		DateTime date;
		NavigateFlags flags;
	}
}
