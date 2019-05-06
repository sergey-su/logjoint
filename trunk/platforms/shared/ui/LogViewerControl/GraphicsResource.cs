using System;
using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Drawing;

namespace LogJoint.UI.LogViewer
{
	public class GraphicsResources
	{
		public Brush DefaultForegroundBrush => defaultForegroundBrushSelector ();
		public Brush DefaultBackgroundBrush => defaultBackgroundBrushSelector ();
		public Brush SelectedBkBrush => selectedBkBrushSelector ();
		public Image ErrorIcon { get; private set; }
		public Image WarnIcon { get; private set; }
		public Image BookmarkIcon { get; private set; }
		public Image FocusedMessageIcon { get; private set; }
		public Pen CursorPen => cursorPenSelector ();
		public Pen TimeSeparatorLine { get; private set; }
		public StringFormat TextFormat { get; private set; }
		public Brush SearchResultHighlightingBackground { get; private set; }
		public Brush SelectionHighlightingBackground { get; private set; }
		public Font Font => fontSelector();
		public Graphics CreateGraphicsForMeasurment() => graphicsForMeasurmentFactory();

		public GraphicsResources(
			IViewModel viewModel,
			Func<FontData, Font> fontFactory,
			StringFormat textFormat,
			(Image error, Image warn, Image bookmark, Image focusedMark) images,
			Func<Graphics> graphicsForMeasurmentFactory
		)
		{
			fontSelector = Selectors.Create(
				() => viewModel.Font,
				fontFactory
			);
			bool isDark () => viewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
			defaultForegroundBrushSelector = Selectors.Create(
				isDark,
				dark => dark ? Brushes.White : Brushes.Black
			);
			defaultBackgroundBrushSelector = Selectors.Create(
				isDark,
				dark => dark ? new Brush(Color.FromArgb (30, 30, 30)) : Brushes.White
			);
			selectedBkBrushSelector = Selectors.Create (
				isDark,
				dark => dark ? new Brush(Color.FromArgb (40, 80, 120)) : new Brush(Color.FromArgb (167, 176, 201))
			);
			cursorPenSelector = Selectors.Create(
				isDark,
				dark => dark ? new Pen (Color.White, 2) : new Pen (Color.Black, 2)
			);
			TimeSeparatorLine = new Pen(Color.Gray, 1);
			TextFormat = textFormat;
			int hightlightingAlpha = 170;
			SearchResultHighlightingBackground = new Brush(Color.FromArgb(hightlightingAlpha, Color.LightSalmon));
			SelectionHighlightingBackground = new Brush(Color.FromArgb(hightlightingAlpha, Color.Cyan));
			ErrorIcon = images.error;
			WarnIcon = images.warn;
			BookmarkIcon = images.bookmark;
			FocusedMessageIcon = images.focusedMark;
			this.graphicsForMeasurmentFactory = graphicsForMeasurmentFactory;
		}

		private readonly Func<Font> fontSelector;
		private readonly Func<Graphics> graphicsForMeasurmentFactory;
		private readonly Func<Brush> defaultForegroundBrushSelector;
		private readonly Func<Brush> defaultBackgroundBrushSelector;
		private readonly Func<Brush> selectedBkBrushSelector;
		private readonly Func<Pen> cursorPenSelector;
	};
}
