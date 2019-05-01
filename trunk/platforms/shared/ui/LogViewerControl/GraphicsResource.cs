using System;
using LogJoint.UI.Presenters.LogViewer;
using LogJoint.Drawing;
using Color = System.Drawing.Color;

namespace LogJoint.UI.LogViewer
{
	public class GraphicsResources
	{
		public Brush DefaultForegroundBrush { get; private set; }
		public Brush DefaultBackgroundBrush { get; private set; }
		public Brush SelectedBkBrush { get; private set; }
		public Image ErrorIcon { get; private set; }
		public Image WarnIcon { get; private set; }
		public Image BookmarkIcon { get; private set; }
		public Image FocusedMessageIcon { get; private set; }
		public Pen CursorPen { get; private set; }
		public Pen TimeSeparatorLine { get; private set; }
		public StringFormat TextFormat { get; private set; }
		public Brush SearchResultHighlightingBackground { get; private set; }
		public Brush SelectionHighlightingBackground { get; private set; }
		public Font Font => font();
		public Graphics CreateGraphicsForMeasurment() => graphicsForMeasurmentFactory();

		public GraphicsResources(
			IViewModel viewModel,
			Func<FontData, Font> fontFactory,
			StringFormat textFormat,
			(Image error, Image warn, Image bookmark, Image focusedMark) images,
			Func<Graphics> graphicsForMeasurmentFactory
		)
		{
			font = Selectors.Create(
				() => viewModel.Font,
				fontFactory
			);
			bool dark = viewModel.ColorTheme == ColorThemeMode.Dark;
			DefaultForegroundBrush = dark ? Brushes.White : Brushes.Black;
			DefaultBackgroundBrush = dark ? new Brush(Color.FromArgb(30, 30, 30)) : Brushes.White;
			SelectedBkBrush = dark ? new Brush(Color.FromArgb(40, 80, 120)) : new Brush(Color.FromArgb(167, 176, 201));
			CursorPen = dark ? new Pen(Color.White, 2) : new Pen(Color.Black, 2);
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

		private readonly Func<Font> font;
		private readonly Func<Graphics> graphicsForMeasurmentFactory;
	};
}
