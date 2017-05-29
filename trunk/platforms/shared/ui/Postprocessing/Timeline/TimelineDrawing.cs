using System;
using LJD = LogJoint.Drawing;
using System.Drawing;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LogJoint.Drawing;
using LogJoint.Postprocessing.Timeline;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public static class Drawing
	{
		public static void DrawCaptionsView(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler,
			int sequenceDiagramAreaWidth,
			Action<string, Rectangle, int, int> drawCaptionWithHighlightedRegion
		)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.CaptionsPanel);

			int availableHeight = viewMetrics.ActivitiesViewHeight;

			foreach (var a in eventsHandler.OnDrawActivities())
			{
				int y = Metrics.GetActivityY(viewMetrics, a.Index);
				if (y < 0)
					continue;
				if (y > availableHeight)
					break;

				var h = viewMetrics.LineHeight - 1;
				var sequenceDiagramTextRect = new Rectangle(0, y, sequenceDiagramAreaWidth, h);
				var textRect = new Rectangle(
					sequenceDiagramTextRect.Right + 3 /* text padding */, y, viewMetrics.ActivitesCaptionsViewWidth - sequenceDiagramTextRect.Right, h);
				var lineRect = new Rectangle(0, y, viewMetrics.ActivitesCaptionsViewWidth, h);
				if (a.IsSelected)
					g.FillRectangle(viewMetrics.SelectedLineBrush, lineRect);
				else if (a.Color.HasValue)
					using (var bgBrush = MakeBrush(a.Color.Value))
						g.FillRectangle(bgBrush, lineRect);
				if (!string.IsNullOrEmpty(a.SequenceDiagramText))
				{
					g.DrawString(a.SequenceDiagramText, viewMetrics.ActivitesCaptionsFont, viewMetrics.ActivitesCaptionsBrush, sequenceDiagramTextRect);
				}
				drawCaptionWithHighlightedRegion (a.Caption, textRect, a.CaptionSelectionBegin, a.CaptionSelectionLength);
			}

			g.PopState ();
		}

		public static void DrawActivtiesView(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler)
		{
			var viewSz = new Size(viewMetrics.ActivitiesViewWidth, viewMetrics.ActivitiesViewHeight);
			Drawing.DrawActivitiesBackground(g, viewMetrics, eventsHandler);
			Drawing.DrawRulerLines(g, viewMetrics, eventsHandler, DrawScope.VisibleRange, viewSz);
			Drawing.DrawActivitiesTopBound(g, viewMetrics);
			Drawing.DrawEvents(g, viewMetrics, eventsHandler);
			Drawing.DrawBookmarks(g, viewMetrics, eventsHandler);
			Drawing.DrawFocusedMessage(g, viewMetrics, eventsHandler, DrawScope.VisibleRange, viewSz);
			Drawing.DrawActivities(g, viewMetrics, eventsHandler);
			Drawing.DrawMeasurer(g, viewMetrics, eventsHandler);
		}

		public static void DrawActivitiesBackground(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler
		)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.ActivitiesPanel);
			foreach (var a in Metrics.GetActivitiesMetrics(viewMetrics, eventsHandler))
			{
				if (a.Activity.IsSelected)
					g.FillRectangle(viewMetrics.SelectedLineBrush, a.ActivityLineRect);
				else if (a.Activity.Color.HasValue)
					using (var bgBrush = MakeBrush(a.Activity.Color.Value))
						g.FillRectangle(bgBrush, a.ActivityLineRect);
			}
			g.PopState ();
		}

		public static void DrawRulerLines(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventHandler,
			DrawScope scope,
			Size viewSize
		)
		{
			double availableWidth = viewSize.Width;
			foreach (var m in Metrics.GetRulerMarks(viewMetrics, eventHandler, scope))
			{
				int x = (int)(m.X * availableWidth);
				g.DrawString(m.Label, viewMetrics.RulerMarkFont, viewMetrics.RulerMarkBrush, new Point(x, 2));
				g.DrawLine(viewMetrics.RulerLinePen, x, 0, x, viewSize.Height);
			}
		}

		public static void DrawActivitiesTopBound(LJD.Graphics g, ViewMetrics viewMetrics)
		{
			int y = viewMetrics.RulersPanelHeight;
			g.DrawLine(viewMetrics.ActivitiesTopBoundPen, 0, y, viewMetrics.ActivitiesViewWidth, y);
		}

		public static void DrawActivities(LJD.Graphics g, ViewMetrics viewMetrics, IViewEvents eventsHandler)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.ActivitiesPanel);

			foreach (var a in Metrics.GetActivitiesMetrics(viewMetrics, eventsHandler))
			{
				g.FillRectangle(GetActivityBrush(viewMetrics, a.Activity.Type), a.ActivityBarRect);

				foreach (var ms in a.Milestones)
					g.DrawLine(viewMetrics.MilestonePen, ms.X, a.ActivityBarRect.Top, ms.X, a.ActivityBarRect.Bottom);

				g.DrawRectangle(viewMetrics.ActivityBarBoundsPen, a.ActivityBarRect);

				if (a.PairedActivityConnectorBounds != null)
				{
					var r = a.PairedActivityConnectorBounds.Value;
					g.DrawLine(viewMetrics.ActivitiesConnectorPen, r.Left, r.Top, r.Left, r.Bottom);
					g.DrawLine(viewMetrics.ActivitiesConnectorPen, r.Right, r.Top, r.Right, r.Bottom);
				}
			}

			g.PopState ();
		}

		public static void DrawEvents(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler)
		{
			foreach (var evt in Metrics.GetEventMetrics(g, eventsHandler, viewMetrics))
			{
				if (evt.VertLinePoints != null)
				{
					g.PushState ();
					g.EnableAntialiasing(true);
					g.DrawLines(viewMetrics.UserEventPen, evt.VertLinePoints);
					g.PopState ();
				}
				else
				{
					g.DrawLine(viewMetrics.UserEventPen, evt.VertLineA, evt.VertLineB);
				}
				if (evt.Icon != null)
					g.DrawImage(evt.Icon, evt.IconRect);
				g.FillRectangle(viewMetrics.EventRectBrush, evt.CaptionRect);
				g.DrawRectangle(viewMetrics.EventRectPen, evt.CaptionRect);
				g.DrawString(evt.Event.Caption, viewMetrics.EventCaptionFont, 
					viewMetrics.EventCaptionBrush, evt.CaptionDrawingOrigin, 
					viewMetrics.EventCaptionStringFormat);
			}
		}

		public static void DrawBookmarks(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler)
		{
			foreach (var evt in Metrics.GetBookmarksMetrics(g, viewMetrics, eventsHandler))
			{
				g.DrawLine(viewMetrics.BookmarkPen, evt.VertLineA, evt.VertLineB);
				if (evt.Icon != null)
					g.DrawImage(evt.Icon, evt.IconRect);
			}
		}

		public static void DrawFocusedMessage(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler, 
			DrawScope scope, 
			Size sz)
		{
			var pos = eventsHandler.OnDrawFocusedMessage(scope);
			if (pos == null)
				return;
			if (pos.Value < 0 || pos.Value > 1)
				return;
			var x = (int)(pos.Value * (double)sz.Width);
			g.DrawLine(viewMetrics.FocusedMessagePen, x, 0, x, sz.Height);
			var img = viewMetrics.FocusedMessageLineTop;
			var imgsz = img.GetSize(height: 4f);
			g.DrawImage(img, new RectangleF
				(x - imgsz.Width/2, 1, imgsz.Width, imgsz.Height));
		}


		public static void DrawMeasurer(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler)
		{
			var drawInfo = eventsHandler.OnDrawMeasurer();
			if (!drawInfo.MeasurerVisible)
				return;
			double viewWidth = viewMetrics.ActivitiesViewWidth;
			int viewHeight = viewMetrics.ActivitiesViewHeight;
			var x1 = Metrics.SafeGetScreenX(drawInfo.X1, viewWidth);
			var x2 = Metrics.SafeGetScreenX(drawInfo.X2, viewWidth);
			g.DrawLine(viewMetrics.MeasurerPen, x1, viewMetrics.MeasurerTop, x1, viewHeight);
			g.DrawLine(viewMetrics.MeasurerPen, x2, viewMetrics.MeasurerTop, x2, viewHeight);
			g.DrawLine(viewMetrics.MeasurerPen, x1, viewMetrics.MeasurerTop, x2, viewMetrics.MeasurerTop);
			var textSz = g.MeasureString(drawInfo.Text, viewMetrics.MeasurerTextFont);
			RectangleF textRect;
			int textHPadding = 2;
			if (textSz.Width < (x2 - x1 - 5))
			{
				textRect = new RectangleF(
					(x2 + x1 - textSz.Width - textHPadding) / 2,
					viewMetrics.MeasurerTop - textSz.Height / 2,
					textSz.Width + textHPadding,
					textSz.Height
				);
			}
			else
			{
				textRect = new RectangleF(
					x2 + 5,
					viewMetrics.MeasurerTop - textSz.Height / 2,
					textSz.Width + textHPadding,
					textSz.Height
				);
				if (textRect.Right > viewMetrics.ActivitiesViewWidth)
				{
					textRect.X = x1 - 5 - textRect.Width;
				}
			}
			g.FillRectangle(viewMetrics.MeasurerTextBoxBrush, textRect);
			g.DrawRectangle(viewMetrics.MeasurerTextBoxPen, new RectangleF(textRect.X, textRect.Y, textRect.Width, textRect.Height));
			g.DrawString(drawInfo.Text,
				viewMetrics.MeasurerTextFont, viewMetrics.MeasurerTextBrush, 
				new PointF((textRect.X + textRect.Right)/2, (textRect.Y + textRect.Bottom)/2),
				viewMetrics.MeasurerTextFormat);
		}

		public static void DrawNavigationPanel(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewEvents eventsHandler)
		{
			var panelClientRect = new Rectangle (0, 0, viewMetrics.NavigationPanelWidth, viewMetrics.NavigationPanelHeight);
			g.FillRectangle(viewMetrics.NavigationPanel_InvisibleBackground, panelClientRect);

			var m = Metrics.GetNavigationPanelMetrics(viewMetrics, eventsHandler);
			g.FillRectangle(viewMetrics.NavigationPanel_VisibleBackground, m.VisibleRangeBox);

			DrawRulerLines(g, viewMetrics, eventsHandler, DrawScope.AvailableRange, panelClientRect.Size);

			double width = (double)viewMetrics.NavigationPanelWidth;

			foreach (var evt in eventsHandler.OnDrawEvents(DrawScope.AvailableRange))
			{
				int x = (int)(evt.X * width);
				g.DrawLine(viewMetrics.UserEventPen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}

			foreach (var evt in eventsHandler.OnDrawBookmarks(DrawScope.AvailableRange))
			{
				int x = (int)(evt.X * width);
				g.DrawLine(viewMetrics.BookmarkPen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}
				
			var focusedMessagePos = eventsHandler.OnDrawFocusedMessage(DrawScope.AvailableRange);
			if (focusedMessagePos.HasValue && focusedMessagePos.Value >= 0 && focusedMessagePos.Value <= 1)
			{
				int x = (int)(focusedMessagePos.Value * width);
				g.DrawLine(viewMetrics.FocusedMessagePen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}

			ResizerDrawing.DrawResizer(g, m.Resizer1, viewMetrics.SystemControlBrush);
			ResizerDrawing.DrawResizer(g, m.Resizer2, viewMetrics.SystemControlBrush);

			Rectangle visibleRangeBox = m.VisibleRangeBox;
			visibleRangeBox.Width = Math.Max(visibleRangeBox.Width, 1);
			g.DrawRectangle(viewMetrics.VisibleRangePen, visibleRangeBox);
		}

		public static LJD.Brush MakeBrush(ModelColor c)
		{
			return new LJD.Brush(Color.FromArgb(c.R, c.G, c.B));
		}

		static LJD.Brush GetActivityBrush(ViewMetrics viewMetrics, ActivityType t)
		{
			switch (t)
			{
			case ActivityType.Lifespan: return viewMetrics.LifetimeBrush;
			case ActivityType.Procedure: return viewMetrics.ProcedureBrush;
			case ActivityType.IncomingNetworking: return viewMetrics.NetworkMessageBrush;
			case ActivityType.OutgoingNetworking: return viewMetrics.NetworkMessageBrush;
			default: return viewMetrics.UnknownActivityBrush;
			}
		}

		static void PushGraphicsStateForDrawingActivites(LJD.Graphics g, ViewMetrics vm, HitTestResult.AreaCode area)
		{
			g.PushState();
			g.IntsersectClip(new Rectangle(0, vm.RulersPanelHeight, 
				area == HitTestResult.AreaCode.CaptionsPanel ? vm.ActivitesCaptionsViewWidth : vm.ActivitiesViewWidth, vm.ActivitiesViewHeight));
		}
	}

	public static class ResizerDrawing
	{
		public static void DrawResizer(LJD.Graphics g, Rectangle bounds, LJD.Brush backgroundBrush)
		{
			g.FillRectangle(backgroundBrush, bounds);
			DrawEllipsis(g, bounds);
		}

		public static void DrawEllipsis(LJD.Graphics g, Rectangle r)
		{
			for (int i = r.Left; i < r.Right - 2; i += 5)
			{
				for (int j = r.Top + 1; j < r.Bottom; j += 5)
				{
					g.FillRectangle(whiteBrush, new Rectangle(i + 1, j + 1, 2, 2));
					g.FillRectangle(darkGray, new Rectangle(i, j, 2, 2));
				}
			}
		}

		static LJD.Brush whiteBrush = new LJD.Brush(Color.White);
		static LJD.Brush darkGray = new LJD.Brush(Color.DarkGray);
	}
}

