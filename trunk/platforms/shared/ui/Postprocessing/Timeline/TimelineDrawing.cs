using System;
using LJD = LogJoint.Drawing;
using System.Drawing;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LogJoint.Drawing;
using LogJoint.Postprocessing.Timeline;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public class ControlDrawing
	{
		private readonly GraphicsResources res;

		public ControlDrawing(GraphicsResources res)
		{
			this.res = res;
		}

		public void DrawCaptionsView(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler,
			Action<string, Rectangle, int, int, bool> drawCaptionWithHighlightedRegion
		)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.CaptionsPanel);

			int availableHeight = viewMetrics.ActivitiesViewHeight;
			bool darkMode = eventsHandler.ColorTheme == Presenters.ColorThemeMode.Dark;

			foreach (var a in eventsHandler.OnDrawActivities())
			{
				int y = viewMetrics.GetActivityY(a.Index);
				if (y < 0)
					continue;
				if (y > availableHeight)
					break;

				var h = viewMetrics.LineHeight - 1;
				var sequenceDiagramTextRect = new Rectangle(0, y, viewMetrics.SequenceDiagramAreaWidth, h);
				var foldingAreaRect = new Rectangle(
					sequenceDiagramTextRect.Right + 1 /* box padding */, y + (h - viewMetrics.FoldingAreaWidth ) / 2, viewMetrics.FoldingAreaWidth, viewMetrics.FoldingAreaWidth);
				var textRect = new Rectangle(
					foldingAreaRect.Right + 3 /* text padding */, y, viewMetrics.ActivitesCaptionsViewWidth - foldingAreaRect.Right, h);
				var lineRect = new Rectangle(0, y, viewMetrics.ActivitesCaptionsViewWidth, h);
				if (a.IsSelected)
					g.FillRectangle(res.SelectedActivityBackgroundBrush, lineRect);
				else if (!darkMode && a.Color.HasValue)
					using (var bgBrush = MakeBrush(a.Color.Value))
						g.FillRectangle(bgBrush, lineRect);
				if (!string.IsNullOrEmpty(a.SequenceDiagramText))
				{
					g.DrawString(a.SequenceDiagramText, res.ActivitesCaptionsFont, res.ActivitesCaptionsBrush, sequenceDiagramTextRect);
				}
				if (a.IsFolded.HasValue)
				{
					g.DrawRectangle(res.FoldingSignPen, foldingAreaRect);
					int pad = 2;
					g.DrawLine(res.FoldingSignPen, foldingAreaRect.Left + pad, foldingAreaRect.MidY(), foldingAreaRect.Right - pad, foldingAreaRect.MidY());
					if (a.IsFolded == true)
					{
						g.DrawLine(res.FoldingSignPen, foldingAreaRect.MidX(), foldingAreaRect.Top + pad, foldingAreaRect.MidX(), foldingAreaRect.Bottom - pad);
					}
				}
				drawCaptionWithHighlightedRegion(a.Caption, textRect, a.CaptionSelectionBegin, a.CaptionSelectionLength, a.IsError);
			}

			g.PopState ();
		}

		public void DrawActivtiesView(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler)
		{
			var viewSz = new Size(viewMetrics.ActivitiesViewWidth, viewMetrics.ActivitiesViewHeight);
			DrawActivitiesBackground(g, viewMetrics, eventsHandler);
			DrawRulerLines(g, viewMetrics, eventsHandler, DrawScope.VisibleRange, viewSz);
			DrawActivitiesTopBound(g, viewMetrics);
			DrawEvents(g, viewMetrics, eventsHandler);
			DrawBookmarks(g, viewMetrics, eventsHandler);
			DrawFocusedMessage(g, viewMetrics, eventsHandler, DrawScope.VisibleRange, viewSz);
			DrawActivities(g, viewMetrics, eventsHandler);
			DrawMeasurer(g, viewMetrics, eventsHandler);
		}

		public void DrawActivitiesBackground(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler
		)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.ActivitiesPanel);
			var darkMode = eventsHandler.ColorTheme == Presenters.ColorThemeMode.Dark;
			foreach (var a in viewMetrics.GetActivitiesMetrics(eventsHandler))
			{
				if (a.Activity.IsSelected)
					g.FillRectangle(res.SelectedActivityBackgroundBrush, a.ActivityLineRect);
				else if (!darkMode && a.Activity.Color.HasValue)
					using (var bgBrush = MakeBrush(a.Activity.Color.Value))
						g.FillRectangle(bgBrush, a.ActivityLineRect);
			}
			g.PopState ();
		}

		public void DrawRulerLines(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventHandler,
			DrawScope scope,
			Size viewSize
		)
		{
			double availableWidth = viewSize.Width;
			foreach (var m in viewMetrics.GetRulerMarks(eventHandler, scope))
			{
				int x = (int)(m.X * availableWidth);
				g.DrawString(m.Label, res.RulerMarkFont, res.RulerMarkBrush, new Point(x, 2));
				g.DrawLine(res.RulerLinePen, x, 0, x, viewSize.Height);
			}
		}

		public void DrawActivitiesTopBound(LJD.Graphics g, ViewMetrics viewMetrics)
		{
			int y = viewMetrics.RulersPanelHeight;
			g.DrawLine(res.ActivitiesTopBoundPen, 0, y, viewMetrics.ActivitiesViewWidth, y);
		}

		public void DrawActivities(LJD.Graphics g, ViewMetrics viewMetrics, IViewModel eventsHandler)
		{
			PushGraphicsStateForDrawingActivites (g, viewMetrics, HitTestResult.AreaCode.ActivitiesPanel);
			var darkMode = eventsHandler.ColorTheme == Presenters.ColorThemeMode.Dark;
			foreach (var a in viewMetrics.GetActivitiesMetrics(eventsHandler))
			{
				if (a.Activity.Type == ActivityDrawType.Group)
					continue;

				g.FillRectangle(GetActivityBrush(a.Activity.Type), a.ActivityBarRect);

				foreach (var ph in a.Phases)
				{
					var phaseMargin = a.ActivityBarRect.Height / 3;
					g.FillRectangle(ph.Brush, ph.X1, a.ActivityBarRect.Top + phaseMargin, 
					                Math.Max(ph.X2 - ph.X1, 2), a.ActivityBarRect.Height - phaseMargin - 2);
				}

				foreach (var ms in a.Milestones)
					g.DrawLine(res.MilestonePen, ms.X, a.ActivityBarRect.Top, ms.X, a.ActivityBarRect.Bottom);

				g.DrawRectangle (res.ActivityBarBoundsPen, a.ActivityBarRect);

				if (a.PairedActivityConnectorBounds != null)
				{
					var r = a.PairedActivityConnectorBounds.Value;
					g.DrawLine(res.ActivitiesConnectorPen, r.Left, r.Top, r.Left, r.Bottom);
					g.DrawLine(res.ActivitiesConnectorPen, r.Right, r.Top, r.Right, r.Bottom);
				}
			}

			g.PopState ();
		}

		public void DrawEvents(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler)
		{
			foreach (var evt in viewMetrics.GetEventMetrics(g, eventsHandler))
			{
				if (evt.VertLinePoints != null)
				{
					g.PushState ();
					g.EnableAntialiasing(true);
					g.DrawLines(res.UserEventPen, evt.VertLinePoints);
					g.PopState ();
				}
				else
				{
					g.DrawLine(res.UserEventPen, evt.VertLineA, evt.VertLineB);
				}
				if (evt.Icon != null)
					g.DrawImage(evt.Icon, evt.IconRect);
				g.FillRectangle(res.EventRectBrush, evt.CaptionRect);
				g.DrawRectangle(res.EventRectPen, evt.CaptionRect);
				g.DrawString(evt.Event.Caption, res.EventCaptionFont,
					res.EventCaptionBrush, evt.CaptionDrawingOrigin,
					res.EventCaptionStringFormat);
			}
		}

		public void DrawBookmarks(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler)
		{
			foreach (var evt in viewMetrics.GetBookmarksMetrics(g, eventsHandler))
			{
				g.DrawLine(res.BookmarkPen, evt.VertLineA, evt.VertLineB);
				if (evt.Icon != null)
					g.DrawImage(evt.Icon, evt.IconRect);
			}
		}

		public void DrawFocusedMessage(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler, 
			DrawScope scope, 
			Size sz)
		{
			var pos = eventsHandler.OnDrawFocusedMessage(scope);
			if (pos == null)
				return;
			if (pos.Value < 0 || pos.Value > 1)
				return;
			var x = (int)(pos.Value * (double)sz.Width);
			g.DrawLine(res.FocusedMessagePen, x, 0, x, sz.Height);
			var img = res.FocusedMessageLineTop;
			var imgsz = img.GetSize(height: 4f);
			g.DrawImage(img, new RectangleF
				(x - imgsz.Width/2, 1, imgsz.Width, imgsz.Height));
		}


		public void DrawMeasurer(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler)
		{
			var drawInfo = eventsHandler.OnDrawMeasurer();
			if (!drawInfo.MeasurerVisible)
				return;
			double viewWidth = viewMetrics.ActivitiesViewWidth;
			int viewHeight = viewMetrics.ActivitiesViewHeight;
			var x1 = ViewMetrics.SafeGetScreenX(drawInfo.X1, viewWidth);
			var x2 = ViewMetrics.SafeGetScreenX(drawInfo.X2, viewWidth);
			g.DrawLine(res.MeasurerPen, x1, viewMetrics.MeasurerTop, x1, viewHeight);
			g.DrawLine(res.MeasurerPen, x2, viewMetrics.MeasurerTop, x2, viewHeight);
			g.DrawLine(res.MeasurerPen, x1, viewMetrics.MeasurerTop, x2, viewMetrics.MeasurerTop);
			var textSz = g.MeasureString(drawInfo.Text, res.MeasurerTextFont);
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
			g.FillRectangle(res.MeasurerTextBoxBrush, textRect);
			g.DrawRectangle(res.MeasurerTextBoxPen, new RectangleF(textRect.X, textRect.Y, textRect.Width, textRect.Height));
			g.DrawString(drawInfo.Text,
				res.MeasurerTextFont, res.MeasurerTextBrush, 
				new PointF((textRect.X + textRect.Right)/2, (textRect.Y + textRect.Bottom)/2),
				res.MeasurerTextFormat);
		}

		public void DrawNavigationPanel(
			LJD.Graphics g,
			ViewMetrics viewMetrics,
			IViewModel eventsHandler)
		{
			var panelClientRect = new Rectangle (0, 0, viewMetrics.NavigationPanelWidth, viewMetrics.NavigationPanelHeight);
			g.FillRectangle(res.NavigationPanel_InvisibleBackground, panelClientRect);

			var m = viewMetrics.GetNavigationPanelMetrics(eventsHandler);
			g.FillRectangle(res.NavigationPanel_VisibleBackground, m.VisibleRangeBox);

			DrawRulerLines(g, viewMetrics, eventsHandler, DrawScope.AvailableRange, panelClientRect.Size);

			double width = (double)viewMetrics.NavigationPanelWidth;

			foreach (var evt in eventsHandler.OnDrawEvents(DrawScope.AvailableRange))
			{
				int x = (int)(evt.X * width);
				g.DrawLine(res.UserEventPen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}

			foreach (var evt in eventsHandler.OnDrawBookmarks(DrawScope.AvailableRange))
			{
				int x = (int)(evt.X * width);
				g.DrawLine(res.BookmarkPen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}
				
			var focusedMessagePos = eventsHandler.OnDrawFocusedMessage(DrawScope.AvailableRange);
			if (focusedMessagePos.HasValue && focusedMessagePos.Value >= 0 && focusedMessagePos.Value <= 1)
			{
				int x = (int)(focusedMessagePos.Value * width);
				g.DrawLine(res.FocusedMessagePen, x, 0, x, viewMetrics.NavigationPanelHeight);
			}

			ResizerDrawing.DrawResizer(g, m.Resizer1, res.SystemControlBrush);
			ResizerDrawing.DrawResizer(g, m.Resizer2, res.SystemControlBrush);

			Rectangle visibleRangeBox = m.VisibleRangeBox;
			visibleRangeBox.Width = Math.Max(visibleRangeBox.Width, 1);
			g.DrawRectangle(res.VisibleRangePen, visibleRangeBox);
		}

		public static LJD.Brush MakeBrush(ModelColor c)
		{
			return new LJD.Brush(Color.FromArgb(c.R, c.G, c.B));
		}

		LJD.Brush GetActivityBrush(ActivityDrawType t)
		{
			switch (t)
			{
			case ActivityDrawType.Lifespan: return res.LifetimeBrush;
			case ActivityDrawType.Procedure: return res.ProcedureBrush;
			case ActivityDrawType.Networking: return res.NetworkMessageBrush;
			case ActivityDrawType.Group: return LJD.Brushes.Transparent;
			default: return res.UnknownActivityBrush;
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

