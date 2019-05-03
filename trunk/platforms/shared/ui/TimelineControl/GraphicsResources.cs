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
		public readonly DrawShadowRect SourcesShadow = new DrawShadowRect(Color.Gray);
		public readonly Pen SourcesBorderPen;
		public readonly Pen ContainerControlHintPen;
		public readonly Pen CutLinePen;
		public readonly Pen RulersPen1, RulersPen2;
		public readonly Brush RulersBrush1, RulersBrush2;
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

		public GraphicsResources(
			IViewModel viewModel,
			string mainFontName, float mainFontSize, float smallFontSize,
			Image bookmarkImage)
		{
			bool isDark() => viewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
			backgroundSelector = Selectors.Create (
				isDark,
				dark => dark ?
					new Brush(Color.FromArgb (80, 80, 80)) :
					new Brush(Color.White)
			);
			MainFont = new Font(mainFontName, mainFontSize);

			SourcesBorderPen = new Pen(Color.Black, 1);
			ContainerControlHintPen = new Pen(Color.LightGray, 1);
			CutLinePen = new Pen(Color.DimGray, 1, new float[] { 2, 2 });

			RulersPen1 = new Pen(Color.Gray, 1, new float[] { 1, 3 });
			RulersPen2 = new Pen(Color.Gray, 1, new float[] { 4, 1 });

			//RulersBrush1 = new Brush(Color.White);
			//RulersBrush2 = new Brush(Color.Gray);
			RulersBrush1 = new Brush (Color.Black); // todo
			RulersBrush2 = new Brush (Color.White);

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
