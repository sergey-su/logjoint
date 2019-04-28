using System;
using LogJoint.Drawing;
using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using LogJoint.UI.Presenters.Timeline;

namespace LogJoint.UI.Timeline
{
	class GraphicsResources
	{
		public Brush Background => backgroundSelector ();
		public readonly Font MainFont;
		public readonly Pen LightModeSourcesBorderPen;
		public readonly Brush DarkModeSourceFillBrush;
		public readonly Pen ContainerControlHintPen;
		public readonly float[] CutLinePenPattern;
		public readonly Pen LightModeCutLinePen;
		public Pen RulersPen1 => rulersPen1();
		public Pen RulersPen2 => rulersPen2();
		public Brush RulersBrush1 => rulersBrush1();
		public Brush RulersBrush2 => rulersBrush2();
		public readonly Font RulersFont;
		public readonly Pen BookmarkPen;
		public readonly Pen HiddenBookmarkPen;
		public readonly Image BookmarkImage;
		public readonly Pen CurrentViewTimePen = Pens.Blue;
		public readonly Brush CurrentViewTimeBrush = Brushes.Blue;
		public readonly Point[] HotTrackMarker;
		public readonly Pen HotTrackLinePen;
		public readonly Pen HotTrackRangePen = Pens.Red;
		public readonly Brush HotTrackRangeBrush;
		public readonly StringFormat CenteredFormat;
		public readonly Brush DragAreaBackgroundBrush;
		public readonly Brush DragAreaTextBrush;

		private readonly Func<Brush> backgroundSelector;
		private readonly Func<Pen> rulersPen1;
		private readonly Func<Pen> rulersPen2;
		private readonly Func<Brush> rulersBrush1;
		private readonly Func<Brush> rulersBrush2;

		public GraphicsResources(
			IViewModel viewModel,
			string mainFontName, float mainFontSize, float smallFontSize,
			Image bookmarkImage)
		{
			bool isDark() => viewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
			backgroundSelector = Selectors.Create (
				isDark,
				dark => dark ?
					new Brush(Color.FromArgb (100, 100, 100)) :
					new Brush(Color.White)
			);
			MainFont = new Font(mainFontName, mainFontSize);

			LightModeSourcesBorderPen = new Pen(Color.DimGray, 1);
			DarkModeSourceFillBrush = new Brush(Color.FromArgb(67, 67, 67));
			ContainerControlHintPen = new Pen(Color.LightGray, 1);
			CutLinePenPattern = new float[] { 2, 2 };
			LightModeCutLinePen = new Pen(Color.DimGray, 1, CutLinePenPattern);

			rulersPen1 = Selectors.Create(isDark, dark => new Pen(Color.Gray, 1, new float[] { 1, 3 }));
			rulersPen2 = Selectors.Create(isDark, dark => new Pen(Color.Gray, 1, new float[] { 4, 1 }));

			rulersBrush1 = Selectors.Create(isDark, dark => new Brush (dark ? Color.Black : Color.White));
			rulersBrush2 = Selectors.Create(isDark, dark => new Brush (dark ? Color.White : Color.Gray));

			RulersFont = new Font(mainFontName, smallFontSize);

			var bmkPenColor = Color.FromArgb(0x5b, 0x87, 0xe0);
			BookmarkPen = new Pen(bmkPenColor, 1);
			HiddenBookmarkPen = new Pen(bmkPenColor, 1, new float[] { 10, 3 });
			BookmarkImage = bookmarkImage;

			HotTrackMarker = new [] 
			{
				new Point(4, 0),
				new Point(0, 4),
				new Point(0, -4)
			};
			HotTrackLinePen = new Pen(Color.FromArgb(128, Color.Red), 1);

			HotTrackRangeBrush = new Brush(Color.FromArgb(20, Color.Red));

			CenteredFormat = new StringFormat(System.Drawing.StringAlignment.Center, System.Drawing.StringAlignment.Near);

			DragAreaBackgroundBrush = Brushes.TextBackground;
			DragAreaTextBrush = Brushes.Text;
		}
	};
}
