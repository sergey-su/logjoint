using System;
using LogJoint.Drawing;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LJD = LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
    public class GraphicsResources
    {
        public readonly LJD.Font ActivitesCaptionsFont;
        public readonly LJD.Brush ActivitesCaptionsBrush;

        public readonly LJD.Font ActionCaptionFont, RulerMarkFont;
        public readonly LJD.Image UserIcon, APIIcon, BookmarkIcon;
        public LJD.Brush SelectedActivityBackgroundBrush => selectedActivityBackgroundBrush();
        public readonly LJD.Brush RulerMarkBrush;
        public LJD.Pen RulerLinePen => rulerLinePen();

        public LJD.Brush ProcedureBrush => procedureBrush();
        public LJD.Brush LifetimeBrush => lifetimeBrush();
        public LJD.Brush NetworkMessageBrush => networkMessageBrush();
        public LJD.Brush UnknownActivityBrush => unknownActivityBrush();
        public readonly LJD.Pen ActivitiesTopBoundPen, ActivitiesConnectorPen;
        public LJD.Pen MilestonePen => milestonePen();
        public LJD.Pen ActivityBarBoundsPen => activityBarBoundsPen();
        public readonly LJD.Brush[] PhaseBrushes;

        public readonly LJD.Pen UserEventPen;
        public readonly LJD.Brush EventRectBrush;
        public readonly LJD.Pen EventRectPen;
        public readonly LJD.Brush EventCaptionBrush;
        public readonly LJD.Font EventCaptionFont;
        public readonly LJD.StringFormat EventCaptionStringFormat;

        public readonly LJD.Pen BookmarkPen;

        public readonly LJD.Pen FocusedMessagePen;
        public readonly LJD.Image FocusedMessageLineTop;

        public readonly LJD.Pen MeasurerPen;
        public readonly LJD.Font MeasurerTextFont;
        public readonly LJD.Brush MeasurerTextBrush;
        public readonly LJD.Brush MeasurerTextBoxBrush;
        public readonly LJD.Pen MeasurerTextBoxPen;
        public readonly LJD.StringFormat MeasurerTextFormat;

        public LJD.Brush NavigationPanel_InvisibleBackground => navigationPanel_InvisibleBackground();
        public LJD.Brush NavigationPanel_VisibleBackground => navigationPanel_VisibleBackground();
        public readonly LJD.Brush SystemControlBrush;
        public readonly LJD.Pen VisibleRangePen;

        public LJD.Pen FoldingSignPen => foldingSignPen();

        private readonly Func<LJD.Brush> selectedActivityBackgroundBrush;
        private readonly Func<LJD.Brush> procedureBrush;
        private readonly Func<LJD.Brush> lifetimeBrush;
        private readonly Func<LJD.Brush> networkMessageBrush;
        private readonly Func<LJD.Brush> unknownActivityBrush;

        private readonly Func<LJD.Pen> activityBarBoundsPen;

        private readonly Func<LJD.Brush> navigationPanel_InvisibleBackground;
        private readonly Func<LJD.Brush> navigationPanel_VisibleBackground;

        private readonly Func<LJD.Pen> rulerLinePen;
        private readonly Func<LJD.Pen> foldingSignPen;
        private readonly Func<LJD.Pen> milestonePen;

        public GraphicsResources(
            IViewModel viewModel,
            string fontName,
            float activitesCaptionsFontSize,
            float actionCaptionsFontSize,
            float rulerMarkFontSize,
            LJD.Image userIcon,
            LJD.Image apiIcon,
            LJD.Image bookmarkIcon,
            LJD.Image focusedMessageLineTop,
            float pensScale,
            LJD.Brush systemControlBrush
        )
        {
            bool isDark() => viewModel.ColorTheme == Presenters.ColorThemeMode.Dark;
            ActivitesCaptionsFont = new LJD.Font(fontName, activitesCaptionsFontSize);
            ActivitesCaptionsBrush = LJD.Brushes.Text;

            ActionCaptionFont = new LJD.Font(fontName, actionCaptionsFontSize);
            RulerMarkFont = new LJD.Font(fontName, rulerMarkFontSize);

            UserIcon = userIcon;
            APIIcon = apiIcon;
            BookmarkIcon = bookmarkIcon;

            selectedActivityBackgroundBrush = Selectors.Create(isDark,
                dark => new LJD.Brush(dark ? Color.FromArgb(40, 80, 120) : Color.FromArgb(187, 196, 221)));

            RulerMarkBrush = LJD.Brushes.Text;
            rulerLinePen = Selectors.Create(isDark, dark =>
                new LJD.Pen(dark ? Color.DarkGray : Color.LightGray, 1));

            Color adjust(Color cl, bool dark) => dark ? cl.MakeDarker(60) : cl;
            procedureBrush = Selectors.Create(isDark, dark => new LJD.Brush(adjust(Color.LightBlue, dark)));
            lifetimeBrush = Selectors.Create(isDark, dark => new LJD.Brush(adjust(Color.LightGreen, dark)));
            networkMessageBrush = Selectors.Create(isDark, dark => new LJD.Brush(adjust(Color.LightSalmon, dark)));
            unknownActivityBrush = Selectors.Create(isDark, dark => new LJD.Brush(adjust(Color.LightGray, dark)));
            ActivitiesTopBoundPen = new LJD.Pen(Color.Gray, 1);

            milestonePen = Selectors.Create(isDark,
                dark => new LJD.Pen(dark ? Color.FromArgb(0, 67, 175) : Color.FromArgb(180, Color.SteelBlue), pensScale * 3f));
            activityBarBoundsPen = Selectors.Create(isDark,
                dark => new LJD.Pen(dark ? Color.White : Color.Gray, 1f));
            ActivitiesConnectorPen = new LJD.Pen(Color.DarkGray, pensScale * 1f, new[] { 1f, 1f });

            PhaseBrushes = new LJD.Brush[]
            {
                new LJD.Brush(Color.FromArgb(255, 170, 170, 170)),
                new LJD.Brush(Color.FromArgb(255, 0, 150, 136)),
                new LJD.Brush(Color.FromArgb(255, 63, 72, 204)),
                new LJD.Brush(Color.FromArgb(255, 34, 175, 76)),
            };

            UserEventPen = new LJD.Pen(Color.Salmon, pensScale * 2f);
            EventRectBrush = new LJD.Brush(Color.Salmon);
            EventRectPen = new LJD.Pen(Color.Gray, 1);
            EventCaptionBrush = new LJD.Brush(Color.Black);
            EventCaptionFont = ActionCaptionFont;
            EventCaptionStringFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Far);

            BookmarkPen = new LJD.Pen(Color.FromArgb(0x5b, 0x87, 0xe0), pensScale * 1f);

            FocusedMessagePen = new LJD.Pen(Color.Blue, pensScale * 1f);
            FocusedMessageLineTop = focusedMessageLineTop;

            MeasurerPen = new LJD.Pen(Color.DarkGreen, pensScale * 1f, new[] { 4f, 2f });
            MeasurerTextFont = new LJD.Font(fontName, rulerMarkFontSize);
            MeasurerTextBrush = new LJD.Brush(Color.Black);
            MeasurerTextBoxBrush = new LJD.Brush(Color.White);
            MeasurerTextBoxPen = new LJD.Pen(Color.DarkGreen, 1f);
            MeasurerTextFormat = new LJD.StringFormat(StringAlignment.Center, StringAlignment.Center);

            navigationPanel_InvisibleBackground = Selectors.Create(isDark,
                dark => dark ? LJD.Brushes.TextBackground : new LJD.Brush(Color.FromArgb(235, 235, 235)));
            navigationPanel_VisibleBackground = Selectors.Create(isDark,
                dark => new LJD.Brush(dark ? Color.FromArgb(60, 60, 60) : Color.White));
            SystemControlBrush = systemControlBrush;
            VisibleRangePen = new LJD.Pen(Color.Gray, 1f);

            foldingSignPen = Selectors.Create(isDark,
                dark => dark ? LJD.Pens.White : LJD.Pens.Black);
        }
    };
}
