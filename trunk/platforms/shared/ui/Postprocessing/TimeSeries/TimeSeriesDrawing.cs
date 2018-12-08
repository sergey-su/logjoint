using System;
using LJD = LogJoint.Drawing;
using System.Drawing;
using System.Linq;
using LogJoint.Drawing;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public static class Drawing
	{
		public class Resources
		{
			public readonly LJD.Pen GridPen = new LJD.Pen(Color.LightGray, 1);
			public LJD.Pen GetTimeSeriesPen(ModelColor color)
			{
				LJD.Pen ret;
				if (!pensCache.TryGetValue(color, out ret))
					pensCache.Add(color, ret = new LJD.Pen(color.ToColor(), 1));
				return ret;
			}
			public readonly LJD.Font AxesFont;
			public readonly LJD.Pen AxesPen;
			public readonly LJD.Brush DataPointLabelBrush;
			public readonly LJD.StringFormat XAxisPointLabelFormat;
			public readonly LJD.StringFormat YAxisPointLabelFormat;
			public readonly LJD.StringFormat YAxisLabelFormat;

			public readonly LJD.Pen BookmarkPen;
			public readonly LJD.Image BookmarkIcon;
			public readonly LJD.Brush BookmarkBrush;
			public readonly LJD.Brush BookmarksGroupGrush;

			public readonly LJD.Pen ParsedEventPen;
			public readonly LJD.Image ParsedEventIcon;
			public readonly LJD.Brush ParsedEventBrush;
			public readonly LJD.Brush ParsedEventsGroupBrush;

			public readonly LJD.Font EventTextFont;
			public readonly LJD.StringFormat EventTextFormat;
			public readonly LJD.Font GroupCaptionFont;

			// todo: adjust sizes for diff. platforms
			public readonly float MajorAxisMarkSize = 5;
			public readonly float MinorAxisMarkSize = 3;
			public readonly float YAxesPadding = 6;
			public readonly float MarkerSize = 3;

			public Resources(string fontName, float fontBaseSize, LJD.Image bookmarkIcon)
			{
				AxesFont = new LJD.Font(fontName, fontBaseSize);
				AxesPen = LJD.Pens.DarkGray;
				XAxisPointLabelFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Far);
				YAxisPointLabelFormat = new LJD.StringFormat(StringAlignment.Far, StringAlignment.Center);
				YAxisLabelFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Far);
				DataPointLabelBrush = LJD.Brushes.Text;
				var bmkColor = Color.FromArgb(0x5b, 0x87, 0xe0);
				BookmarkPen = new LJD.Pen(bmkColor, 1);
				BookmarkBrush = new LJD.Brush(bmkColor);
				BookmarkIcon = bookmarkIcon;
				EventTextFont = new LJD.Font(fontName, fontBaseSize * 0.8f);
				EventTextFormat = new LJD.StringFormat(StringAlignment.Far, StringAlignment.Far);
				ParsedEventPen = new LJD.Pen(Color.Red, 1);
				ParsedEventBrush = new LJD.Brush(Color.Red);
				GroupCaptionFont = EventTextFont;
				ParsedEventsGroupBrush = new LJD.Brush(Color.FromArgb(100, Color.Red));
				BookmarksGroupGrush = new LJD.Brush(Color.FromArgb(100, Color.Blue));
			}


			private readonly Dictionary<ModelColor, LJD.Pen> pensCache = new Dictionary<ModelColor, LJD.Pen>();
		};

		public static void DrawPlotsArea(
			LJD.Graphics g, 
			Resources resources, 
			PlotsDrawingData pdd, 
			PlotsViewMetrics m
		)
		{
			g.DrawRectangle(resources.AxesPen, new RectangleF(new PointF(), m.Size));
				
			foreach (var x in pdd.XAxis.Points)
				g.DrawLine(resources.GridPen, new PointF(x.Position, 0), new PointF(x.Position, m.Size.Height));

			g.PushState();
			g.EnableAntialiasing(true);
			foreach (var s in pdd.TimeSeries)
			{
				var pen = resources.GetTimeSeriesPen(s.Color);
				var prevPt = new PointF();
				bool isFirstPt = true;
				foreach (var pt in s.Points)
				{
					if (!isFirstPt && s.DrawLine)
					{
						g.DrawLine (pen, prevPt, pt);
					}
					prevPt = pt;
					DrawPlotMarker (g, resources, pen, pt, s.Marker);
					isFirstPt = false;
				}
			}
			foreach (var e in pdd.Events)
			{
				if ((e.Type & EventDrawingData.EventType.Group) != 0)
				{
					var captionSz = g.MeasureString(e.Text, resources.GroupCaptionFont);
					var round = 2f;
					captionSz.Width += round*2;
					captionSz.Height += round*2;
					var captionRect = new RectangleF(
						e.X + e.Width/2 - captionSz.Width/2, 1, captionSz.Width, captionSz.Height);
					var vertLineRect = new RectangleF(e.X, captionRect.Top, e.Width, m.Size.Height);

					if ((e.Type & EventDrawingData.EventType.ParsedEvent) != 0)
						g.FillRectangle(resources.ParsedEventsGroupBrush, vertLineRect);
					if ((e.Type & EventDrawingData.EventType.Bookmark) != 0)
						g.FillRectangle(resources.BookmarksGroupGrush, vertLineRect);

					g.FillRoundRectangle(LJD.Brushes.Red, captionRect, round);
					g.DrawRoundRectangle(LJD.Pens.White, captionRect, round);
					g.DrawString(e.Text, resources.GroupCaptionFont, LJD.Brushes.White, 
						new PointF(captionRect.X + round, captionRect.Y + round));
				}
				else
				{
					LJD.Pen pen;
					LJD.Brush brush;
					LJD.Image icon;
					if ((e.Type & EventDrawingData.EventType.Bookmark) != 0)
					{
						pen = resources.BookmarkPen;
						brush = resources.BookmarkBrush;
						icon = resources.BookmarkIcon;
					}
					else
					{
						pen = resources.ParsedEventPen;
						brush = resources.ParsedEventBrush;
						icon = resources.ParsedEventIcon;
					}
					g.DrawLine(pen, new PointF(e.X, 0), new PointF(e.X, m.Size.Height));
					if (icon != null)
					{
						float iconWidth = 10; // todo: hardcoded
						g.DrawImage(icon, new RectangleF(
							e.X - iconWidth/2, 1, iconWidth, iconWidth*icon.Height/icon.Width));
					}
					if (e.Text != null)
					{
						g.PushState();
						g.TranslateTransform(e.X, 6);
						g.RotateTransform(-90);
						g.DrawString(e.Text, resources.EventTextFont, brush, 
							new PointF(), resources.EventTextFormat);
						g.PopState();
					}
				}
			}
			g.PopState();

			if (pdd.FocusedMessageX != null)
			{
				g.DrawLine(LJD.Pens.Blue, new PointF(pdd.FocusedMessageX.Value, 0), new PointF(pdd.FocusedMessageX.Value, m.Size.Height));
			}

			pdd.UpdateThrottlingWarning();
		}

		public static void DrawLegendSample(
			LJD.Graphics g,
			Resources resources,
			ModelColor color,
			MarkerType markerType,
			RectangleF rect
		)
		{
			g.PushState();
			g.EnableAntialiasing(true);
			var pen = resources.GetTimeSeriesPen(color);
			var midX = (rect.X + rect.Right) / 2;
			var midY = (rect.Y + rect.Bottom) / 2;
			g.DrawLine(pen, rect.X, midY, rect.Right, midY);
			DrawPlotMarker(g, resources, pen, new PointF(midX, midY), markerType);
			g.PopState();
		}

		static void DrawPlotMarker(LJD.Graphics g, Resources resources, LJD.Pen pen, PointF p, MarkerType markerType)
		{
			float markerSize = resources.MarkerSize;
			switch (markerType)
			{
				case MarkerType.Cross:
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y - markerSize), new PointF(p.X + markerSize, p.Y + markerSize));
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y + markerSize), new PointF(p.X + markerSize, p.Y - markerSize));
					break;
				case MarkerType.Circle:
					g.DrawEllipse(pen, new RectangleF(p.X - markerSize, p.Y - markerSize, markerSize * 2, markerSize * 2));
					break;
				case MarkerType.Square:
					g.DrawRectangle(pen, new RectangleF(p.X - markerSize, p.Y - markerSize, markerSize * 2, markerSize * 2));
					break;
				case MarkerType.Diamond:
					g.DrawLines(pen, new[] 
					{
						new PointF(p.X - markerSize, p.Y),
						new PointF(p.X, p.Y - markerSize),
						new PointF(p.X + markerSize, p.Y),
						new PointF(p.X, p.Y + markerSize),
						new PointF(p.X - markerSize, p.Y),
					});
					break;
				case MarkerType.Triangle:
					g.DrawLines(pen, new[]
					{
						new PointF(p.X - markerSize, p.Y + markerSize/2),
						new PointF(p.X, p.Y - markerSize),
						new PointF(p.X + markerSize, p.Y + markerSize/2),
						new PointF(p.X - markerSize, p.Y + markerSize/2),
					});
					break;
				case MarkerType.Plus:
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y), new PointF(p.X + markerSize, p.Y));
					g.DrawLine(pen, new PointF(p.X, p.Y - markerSize), new PointF(p.X, p.Y + markerSize));
					break;
				case MarkerType.Star:
					// plus
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y), new PointF(p.X + markerSize, p.Y));
					g.DrawLine(pen, new PointF(p.X, p.Y - markerSize), new PointF(p.X, p.Y + markerSize));
					// cross
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y - markerSize), new PointF(p.X + markerSize, p.Y + markerSize));
					g.DrawLine(pen, new PointF(p.X - markerSize, p.Y + markerSize), new PointF(p.X + markerSize, p.Y - markerSize));
					break;
			}
		}

		public static void DrawXAxis(LJD.Graphics g, Resources resources, PlotsDrawingData pdd, float height)
		{
			var majorMarkerHeight = (height - g.MeasureString("123", resources.AxesFont).Height) * 3f / 4f;
			var minorMarkerHeight = majorMarkerHeight / 2f;

			foreach (var x in pdd.XAxis.Points)
			{
				g.DrawLine(
					resources.AxesPen,
					new PointF(x.Position, 0),
					new PointF(x.Position, x.IsMajorMark ? majorMarkerHeight : minorMarkerHeight)
				);
				g.DrawString(x.Label, resources.AxesFont, resources.DataPointLabelBrush, new PointF(x.Position, height), resources.XAxisPointLabelFormat);
			}
		}

		public static void DrawYAxes(LJD.Graphics g, Resources resources, PlotsDrawingData pdd, float yAxesAreaWidth, PlotsViewMetrics m)
		{
			float x = yAxesAreaWidth;
			var font = resources.AxesFont;
			foreach (var axis in pdd.YAxes)
			{
				float maxLabelWidth = 0;
				foreach (var p in axis.Points)
				{
					var pt = new PointF(x - (p.IsMajorMark ? resources.MajorAxisMarkSize : resources.MinorAxisMarkSize), p.Position);
					g.DrawLine(resources.AxesPen, pt, new PointF(x, p.Position));
					if (p.Label != null)
						g.DrawString(p.Label, font, resources.DataPointLabelBrush, pt, resources.YAxisPointLabelFormat);
					maxLabelWidth = Math.Max(maxLabelWidth, g.MeasureString(p.Label ?? "", resources.AxesFont).Width);
				}
				x -= (resources.MajorAxisMarkSize + maxLabelWidth);
				g.PushState();
				g.TranslateTransform(x, m.Size.Height / 2);
				g.RotateTransform(-90);
				g.DrawString(axis.Label, resources.AxesFont, resources.DataPointLabelBrush, new PointF(0, 0), resources.YAxisLabelFormat);
				g.PopState();
				x -= (g.MeasureString(axis.Label, resources.AxesFont).Height + resources.YAxesPadding);
			}
		}

		public static string GetYAxisId(LJD.Graphics g, Resources resources, PlotsDrawingData pdd, float xCoordinate, float viewWidth)
		{
			float x = viewWidth;
			foreach (var a in GetYAxesMetrics(g, resources, pdd))
			{
				var x2 = x - a.Width;
				if (xCoordinate > x2)
					return a.AxisData.Id;
				x = x2;
			}
			return null;
		}

		public struct YAxisMetrics
		{
			public AxisDrawingData AxisData;
			public float Width;
		};

		public static IEnumerable<YAxisMetrics> GetYAxesMetrics(LJD.Graphics g, Resources resources, PlotsDrawingData pdd)
		{
			foreach (var axis in pdd.YAxes)
			{
				float maxLabelWidth = 0;
				foreach (var p in axis.Points)
					maxLabelWidth = Math.Max(maxLabelWidth, g.MeasureString(p.Label ?? "", resources.AxesFont).Width);
				float unitTextHeight = g.MeasureString(axis.Label, resources.AxesFont).Height;
				yield return new YAxisMetrics()
				{
					AxisData = axis,
					Width = resources.MajorAxisMarkSize + maxLabelWidth + unitTextHeight + resources.YAxesPadding
				};
			}
		}
	}
}