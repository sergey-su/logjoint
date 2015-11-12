using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Timeline;
using LogJoint.UI.Timeline;

namespace LogJoint.UI
{
	public partial class TimeLineControl : Control, IView
	{
		#region Data
		public const int DragAreaHeight = 5;
		
		IViewEvents viewEvents;

		Size? datesSize;
		Point? dragPoint;
		TimeLineDragForm dragForm;

		Point? lastToolTipPoint;
		bool toolTipVisible = false;

		static readonly GraphicsPath roundRectsPath = new GraphicsPath();
		Resources res = new Resources();
		readonly UIUtils.FocuslessMouseWheelMessagingFilter focuslessMouseWheelMessagingFilter;

		#endregion

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

		#region IView

		void IView.SetEventsHandler(IViewEvents presenter)
		{
			this.viewEvents = presenter;
		}

		void IView.Invalidate()
		{
			base.Invalidate();
		}

		PresentationMetrics IView.GetPresentationMetrics()
		{
			return ToPresentationMetrics(GetMetrics());
		}

		HitTestResult IView.HitTest(int x, int y)
		{
			var pt = new Point(x, y);
			var m = GetMetrics();

			if (m.TimeLine.Contains(pt))
				return new HitTestResult() { Area = ViewArea.Timeline };
			else if (m.TopDate.Contains(pt))
				return new HitTestResult() { Area = ViewArea.TopDate };
			else if (m.BottomDate.Contains(pt))
				return new HitTestResult() { Area = ViewArea.BottomDate };
			else if (m.TopDrag.Contains(pt))
				return new HitTestResult() { Area = ViewArea.TopDrag };
			else if (m.BottomDrag.Contains(pt))
				return new HitTestResult() { Area = ViewArea.BottomDrag };
			else
				return new HitTestResult() { Area = ViewArea.None };
		}

		void IView.TryBeginDrag(int x, int y)
		{
			dragPoint = new Point(x, y);
			viewEvents.OnBeginTimeRangeDrag();
		}

		void IView.ResetToolTipPoint(int x, int y)
		{
			if (lastToolTipPoint == null
			 || (Math.Abs(lastToolTipPoint.Value.X - x) + Math.Abs(lastToolTipPoint.Value.Y - y)) > 4)
			{
				OnResetToolTip();
			}
		}

		void IView.UpdateDragViewPositionDuringAnimation(int y, bool topView)
		{
			bool animateDragForm = dragForm != null && dragForm.Visible;
			if (animateDragForm)
			{
				if (topView)
				{
					dragForm.Top = this.PointToScreen(new Point(0, y)).Y - dragForm.Height;
				}
				else
				{
					dragForm.Top = this.PointToScreen(new Point(0, y)).Y;
				}
			}
		}

		void IView.RepaintNow()
		{
			this.Refresh();
		}

		void IView.InterrupDrag()
		{
			StopDragging(false);
		}

		#endregion

		#region Control overrides

		protected override void OnPaint(PaintEventArgs pe)
		{
			Graphics g = pe.Graphics;

			g.FillRectangle(res.Background, pe.ClipRectangle);

			Metrics m = GetMetrics();

			var drawInfo = viewEvents.OnDraw(ToPresentationMetrics(m));
			if (drawInfo == null)
				return;

			DrawSources(g, drawInfo);
			DrawRulers(g, m, drawInfo);
			DrawDragAreas(g, m, drawInfo);
			DrawBookmarks(g, m, drawInfo);
			DrawCurrentViewTime(g, m, drawInfo);
			DrawHotTrackRange(g, m, drawInfo);
			DrawHotTrackDate(g, m, drawInfo);
			DrawFocusRect(g, drawInfo);

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

		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.Focus();

			if (e.Button == MouseButtons.Left)
				viewEvents.OnLeftMouseDown(e.X, e.Y);
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			Point pt = this.PointToClient(Control.MousePosition);
			viewEvents.OnMouseDblClick(pt.X, pt.Y);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!this.Capture)
				StopDragging(false);

			if (viewEvents == null)
			{
				this.Cursor = Cursors.Default;
				return;
			}

			Metrics m = GetMetrics();
			if (dragPoint.HasValue)
			{
				Point mousePt = this.PointToScreen(new Point(dragPoint.Value.X, e.Y));

				if (dragForm == null)
					dragForm = new TimeLineDragForm(this);

				ViewArea area = m.TopDrag.Contains(dragPoint.Value) ? ViewArea.TopDrag : ViewArea.BottomDrag;
				dragForm.Area = area;

				var rslt = viewEvents.OnDragging(
					area,
					e.Y - dragPoint.Value.Y +
						(area == ViewArea.TopDrag ? m.TimeLine.Top : m.TimeLine.Bottom)
				);
				DateTime d = rslt.D;
				dragForm.Date = d;

				Point pt1 = this.PointToScreen(new Point());
				Point pt2 = this.PointToScreen(new Point(ClientSize.Width, 0));
				int formHeight = GetDatesSize().Height + DragAreaHeight;
				dragForm.SetBounds(
					pt1.X,
					pt1.Y + rslt.Y +
						(area == ViewArea.TopDrag ? -formHeight : 0),
					pt2.X - pt1.X,
					formHeight
				);

				if (!dragForm.Visible)
				{
					dragForm.Visible = true;
					this.Focus();
				}
			}
			else
			{
				var cursor = viewEvents.OnMouseMove(e.X, e.Y);

				if (cursor == CursorShape.SizeNS)
					this.Cursor = Cursors.SizeNS;
				else if (cursor == CursorShape.Wait)
					this.Cursor = Cursors.WaitCursor;
				else
					this.Cursor = Cursors.Arrow;
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			viewEvents.OnMouseWheel(e.X, e.Y, e.Delta, Control.ModifierKeys == Keys.Control);
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

		protected override void OnMouseLeave(EventArgs e)
		{
			viewEvents.OnMouseLeave();
			base.OnMouseLeave(e);
		}

		#endregion

		#region Control's event handlers

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			var pt = PointToClient(Control.MousePosition);
			var menuData = viewEvents.OnContextMenu(pt.X, pt.Y);

			if (menuData == null)
			{
				e.Cancel = true;
				return;
			}

			resetTimeLineMenuItem.Enabled = menuData.ResetTimeLineMenuItemEnabled;
			viewTailModeMenuItem.Checked = menuData.ViewTailModeMenuItemChecked;

			zoomToMenuItem.Text = menuData.ZoomToMenuItemText ?? "";
			zoomToMenuItem.Visible = menuData.ZoomToMenuItemText != null;
			zoomToMenuItem.Tag = menuData.ZoomToMenuItemData;
		}

		private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (e.ClickedItem == resetTimeLineMenuItem)
			{
				viewEvents.OnResetTimeLineMenuItemClicked();
			}
			else if (e.ClickedItem == viewTailModeMenuItem)
			{
				viewEvents.OnViewTailModeMenuItemClicked(viewTailModeMenuItem.Checked);
			}
			else if (e.ClickedItem == zoomToMenuItem)
			{
				viewEvents.OnZoomToMenuItemClicked(zoomToMenuItem.Tag);
			}
		}

		private void toolTipTimer_Tick(object sender, EventArgs e)
		{
			ShowToolTip();
			toolTipTimer.Stop();
		}

		private void contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			viewEvents.OnContextMenuClosed();
		}

		#endregion

		#region Implementation

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

		static int GetSourceBarWidth(int srcLeft, int srcRight)
		{
			int sourceBarWidth = srcRight - srcLeft - StaticMetrics.SourceShadowSize.Width;
			return sourceBarWidth;
		}

		void DrawSources(Graphics g, DrawInfo drawInfo)
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
			}
		}

		void DrawRulers(Graphics g, Metrics m, DrawInfo drawInfo)
		{
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

			foreach (var rm in drawInfo.RulerMarks)
			{
				int y = rm.Y;
				g.DrawLine(rm.IsMajor ? res.RulersPen2 : res.RulersPen1, 0, y, m.Client.Width, y);
				if (rm.Label != null)
				{
					g.DrawString(rm.Label, res.RulersFont, Brushes.White, 3 + 1, y + 1);
					g.DrawString(rm.Label, res.RulersFont, Brushes.Gray, 3, y);
				}
			}

			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
		}

		public void DrawDragArea(Graphics g, DateTime timestamp, int x1, int x2, int y)
		{
			DrawDragArea(g, viewEvents.OnDrawDragArea(timestamp), x1, x2, y);
		}

		void DrawDragArea(Graphics g, DragAreaDrawInfo di, int x1, int x2, int y)
		{
			int center = (x1 + x2) / 2;
			string fullTimestamp = di.LongText;
			if (g.MeasureString(fullTimestamp, this.Font).Width < (x2 - x1))
				g.DrawString(fullTimestamp, 
					this.Font, Brushes.Black, center, y, res.CenteredFormat);
			else
				g.DrawString(di.ShortText,
					this.Font, Brushes.Black, center, y, res.CenteredFormat);
		}

		void DrawDragAreas(Graphics g, Metrics m, DrawInfo di)
		{
			g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
				0, m.TopDrag.Y, m.Client.Width, m.TopDate.Bottom - m.TopDrag.Y
			));
			DrawDragArea(g, di.TopDragArea, m.Client.Left, m.Client.Right, m.TopDate.Top);
			UIUtils.DrawDragEllipsis(g, m.TopDrag);

			g.FillRectangle(SystemBrushes.ButtonFace, new Rectangle(
				0, m.BottomDate.Y, m.Client.Width, m.BottomDrag.Bottom - m.BottomDate.Y
			));
			DrawDragArea(g, di.BottomDragArea, m.Client.Left, m.Client.Right, m.BottomDate.Top);
			UIUtils.DrawDragEllipsis(g, m.BottomDrag);
		}

		void DrawBookmarks(Graphics g, Metrics m, DrawInfo di)
		{
			foreach (var bmk in di.Bookmarks)
			{
				int y = bmk.Y;
				bool hidden = bmk.IsHidden;
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

		void DrawCurrentViewTime(Graphics g, Metrics m, DrawInfo di)
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

		void DrawHotTrackDate(Graphics g, Metrics m, DrawInfo di)
		{
			if (di.HotTrackDate == null)
				return;
			int y = di.HotTrackDate.Value.Y;
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

		void DrawHotTrackRange(Graphics g, Metrics m, DrawInfo di)
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

		void DrawFocusRect(Graphics g, DrawInfo di)
		{
			if (Focused)
			{
				if (di.FocusRectIsRequired)
				{
					ControlPaint.DrawFocusRectangle(g, this.ClientRectangle);
				}
			}
		}

		static PresentationMetrics ToPresentationMetrics(Metrics m)
		{
			return new PresentationMetrics()
			{
				X = m.TimeLine.X,
				Y = m.TimeLine.Y,
				Width = m.TimeLine.Width,
				Height = m.TimeLine.Height,
				DistanceBetweenSources = StaticMetrics.DistanceBetweenSources,
				SourcesHorizontalPadding = StaticMetrics.SourcesHorizontalPadding,
				MinimumTimeSpanHeight = StaticMetrics.MinimumTimeSpanHeight
			};
		}

		Size GetDatesSize()
		{
			if (datesSize != null)
				return datesSize.Value;
			using (Graphics g = this.CreateGraphics())
			{
				SizeF tmp = g.MeasureString("0123", this.Font);
				datesSize = new Size((int)Math.Ceiling(tmp.Width),
					(int)Math.Ceiling(tmp.Height));
			}
			return datesSize.Value;
		}

		void StopDragging(bool accept)
		{
			if (dragPoint.HasValue)
			{
				DateTime? date = null;
				bool isFromTopDragArea = false;
				if (accept && dragForm != null && dragForm.Visible)
				{
					if (dragForm.Area == ViewArea.TopDrag)
					{
						date = dragForm.Date;
						isFromTopDragArea = true;
					}
					else
					{
						date = dragForm.Date;
						isFromTopDragArea = false;
					}
				}
				dragPoint = new Point?();
				viewEvents.OnEndTimeRangeDrag(date, isFromTopDragArea);
			}
			if (dragForm != null && dragForm.Visible)
			{
				dragForm.Visible = false;
			}
		}

		Metrics GetMetrics()
		{
			Metrics r;
			r.Client = this.ClientRectangle;
			r.TopDrag = new Rectangle(DragAreaHeight / 2, 0, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.TopDate = new Rectangle(0, r.TopDrag.Bottom, r.Client.Width, GetDatesSize().Height);
			r.BottomDrag = new Rectangle(DragAreaHeight / 2, r.Client.Height - DragAreaHeight, r.Client.Width - DragAreaHeight, DragAreaHeight);
			r.BottomDate = new Rectangle(0, r.BottomDrag.Top - GetDatesSize().Height, r.Client.Width, GetDatesSize().Height);
			r.TimeLine = new Rectangle(0, r.TopDate.Bottom, r.Client.Width,
				r.BottomDate.Top - r.TopDate.Bottom - StaticMetrics.SourceShadowSize.Height - StaticMetrics.SourcesBottomPadding);
			return r;
		}

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
			var tooltip = viewEvents.OnTooltip(clientPt.X, clientPt.Y);
			if (tooltip == null)
				return;
			lastToolTipPoint = clientPt;
			Cursor cursor = this.Cursor;
			if (cursor != null)
			{
				pt.Y += cursor.Size.Height - cursor.HotSpot.Y;
			}
			toolTip.Show(tooltip, this, PointToClient(pt));
			toolTipVisible = true;
		}

		void OnResetToolTip()
		{
			HideToolTip();
			toolTipTimer.Stop();
			toolTipTimer.Start();
		}

		#endregion

		#region Helper classes

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

		#endregion
	}
}
