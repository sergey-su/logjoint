using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LogJoint.UI.Timeline
{
	class Resources : IDisposable
	{
		public readonly Brush Background = SystemBrushes.Window;
		public readonly UIUtils.DrawShadowRect SourcesShadow = new UIUtils.DrawShadowRect(Color.Gray);
		public readonly Pen SourcesBorderPen = Pens.DimGray;
		public readonly Pen CutLinePen;
		public readonly Pen RulersPen1, RulersPen2;
		public readonly Font RulersFont;
		public readonly Pen BookmarkPen;
		public readonly Pen HiddenBookmarkPen;
		public readonly Pen CurrentViewTimePen = Pens.Blue;
		public readonly Brush CurrentViewTimeBrush = Brushes.Blue;
		public readonly GraphicsPath HotTrackMarker;
		public readonly Pen HotTrackLinePen;
		public readonly Pen HotTrackRangePen = Pens.Red;
		public readonly Brush HotTrackRangeBrush;
		public readonly StringFormat CenteredFormat;

		public Resources()
		{
			CutLinePen = new Pen(SourcesBorderPen.Color);
			CutLinePen.DashPattern = new float[] { 2, 2 };

			RulersPen1 = new Pen(Color.Gray, 1);
			RulersPen1.DashPattern = new float[] { 1, 3 };
			RulersPen2 = new Pen(Color.Gray, 1);
			RulersPen2.DashPattern = new float[] { 4, 1 };
			RulersFont = new Font("Tahoma", 6);

			BookmarkPen = new Pen(Color.FromArgb(0x5b, 0x87, 0xe0));
			HiddenBookmarkPen = (Pen)BookmarkPen.Clone();
			HiddenBookmarkPen.DashPattern = new float[] { 10, 3 };

			HotTrackMarker = new GraphicsPath();
			HotTrackMarker.AddPolygon(new Point[] {
					new Point(4, 0),
					new Point(0, 4),
					new Point(0, -4)
				});
			HotTrackLinePen = new Pen(Color.FromArgb(128, Color.Red), 1);

			HotTrackRangeBrush = new SolidBrush(Color.FromArgb(20, Color.Red));

			CenteredFormat = new StringFormat();
			CenteredFormat.Alignment = StringAlignment.Center;
		}

		public void Dispose()
		{
			SourcesShadow.Dispose();
			CutLinePen.Dispose();
			RulersPen1.Dispose();
			RulersPen2.Dispose();
			RulersFont.Dispose();
			BookmarkPen.Dispose();
			HiddenBookmarkPen.Dispose();
			HotTrackMarker.Dispose();
			HotTrackLinePen.Dispose();
			HotTrackRangeBrush.Dispose();
			CenteredFormat.Dispose();
		}
	};
}
