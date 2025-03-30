using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LogJoint.Drawing;
using LogJoint.Settings;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;

namespace LogJoint.UI.Presenters.Options.Appearance
{
    public class Presenter : IPresenter, IViewModel, IPresenterInternal
    {
        public Presenter(
            Settings.IGlobalSettingsAccessor settings,
            LogViewer.IPresenterFactory logViewerPresenterFactory,
            IChangeNotification changeNotification,
            IColorTheme theme
        )
        {
            this.settingsAccessor = settings;

            this.sampleMessagesBaseTime = DateTime.UtcNow;
            this.temporaryColorTheme = new TemporaryColorTheme(theme, changeNotification);
            this.sampleThreads = new ModelThreads(new ColorLease(temporaryColorTheme.ThreadColorsCount));
            this.dummyModel = new LogViewer.DummyModel();
            this.sampleLogViewerPresenter = logViewerPresenterFactory.CreateIsolatedPresenter(
                dummyModel, null,
                theme: temporaryColorTheme);
            this.sampleLogViewerPresenter.ShowTime = false;
            this.sampleLogViewerPresenter.ShowRawMessages = false;
            this.sampleLogViewerPresenter.DisabledUserInteractions =
                LogViewer.UserInteraction.FontResizing |
                LogViewer.UserInteraction.RawViewSwitching |
                LogViewer.UserInteraction.FramesNavigationMenu |
                LogViewer.UserInteraction.CopyMenu;

            this.fontSizeControl = new LabeledStepperPresenter.Presenter(changeNotification);
            this.fontSizeControl.OnValueChanged += (sender, e) => UpdateSampleLogView(fullUpdate: false);
        }


        void IPresenter.Load()
        {
        }

        bool IPresenter.Apply()
        {
            settingsAccessor.Appearance = new Settings.Appearance(
                coloring: ReadColoringModeControl(),
                fontFamily: ReadFontNameControl(),
                fontSize: ReadFontSizeControl(),
                coloringBrightness: ReadColoringPaletteControl(),
                theme: Settings.Appearance.ColorTheme.Light
            );
            return true;
        }

        void IViewModel.OnSelectedValueChanged(ViewControl ctrl)
        {
            UpdateSampleLogView(fullUpdate: ctrl == ViewControl.PaletteSelector);
        }

        LabeledStepperPresenter.IViewModel IViewModel.FontSizeControl => fontSizeControl;

        void IViewModel.SetView(IView view)
        {
            this.view = view;
            InitView();
            UpdateSampleLogView(fullUpdate: true);
        }

        LogViewer.IViewModel IViewModel.LogView => sampleLogViewerPresenter;

        void InitView()
        {
            var appearance = settingsAccessor.Appearance;

            view.SetSelectorControl(ViewControl.ColoringSelector, coloringModes, (int)appearance.Coloring);

            view.SetSelectorControl(ViewControl.FontFamilySelector, view.AvailablePreferredFamilies,
                view.AvailablePreferredFamilies.IndexOf(f => string.Compare(f, appearance.FontFamily ?? "", true) == 0).GetValueOrDefault(0));

            fontSizeControl.AllowedValues =
                view.FontSizes
                    .Select(p => p.Value)
                    .ToArray();
            fontSizeControl.Value =
                view.FontSizes
                    .Where(p => p.Key == appearance.FontSize)
                    .Select(p => p.Value)
                    .FirstOrDefault(view.FontSizes[0].Value);

            view.SetSelectorControl(ViewControl.PaletteSelector, coloringPalettes, (int)appearance.ColoringBrightness);
        }

        void FillSampleMessagesCollection()
        {
            foreach (var t in sampleThreads.Items)
                sampleThreads.UnregisterThread(t);

            var sampleMessagesCollection = new List<IMessage>();
            DateTime baseTime = sampleMessagesBaseTime;
            var t1 = sampleThreads.RegisterThread("thread1", null);
            var t2 = sampleThreads.RegisterThread("thread2", null);
            var t3 = sampleThreads.RegisterThread("thread3", null);
            sampleMessagesCollection.Add(new Message(0, 1, t1, new MessageTimestamp(baseTime.AddSeconds(0)), new StringSlice("sample message 0"), SeverityFlag.Info));
            sampleMessagesCollection.Add(new Message(1, 2, t2, new MessageTimestamp(baseTime.AddSeconds(1)), new StringSlice("sample message 1"), SeverityFlag.Info));
            sampleMessagesCollection.Add(new Message(2, 3, t1, new MessageTimestamp(baseTime.AddSeconds(2)), new StringSlice("warning: sample message 2"), SeverityFlag.Warning));
            sampleMessagesCollection.Add(new Message(3, 4, t3, new MessageTimestamp(baseTime.AddSeconds(3)), new StringSlice("sample message 3"), SeverityFlag.Info));
            sampleMessagesCollection.Add(new Message(4, 5, t2, new MessageTimestamp(baseTime.AddSeconds(4)), new StringSlice("error: sample message 4"), SeverityFlag.Error));
            sampleMessagesCollection.Add(new Message(5, 6, t1, new MessageTimestamp(baseTime.AddSeconds(5)), new StringSlice("sample message 5"), SeverityFlag.Info));

            dummyModel.SetMessages(sampleMessagesCollection);
        }

        void UpdateSampleLogView(bool fullUpdate)
        {
            sampleLogViewerPresenter.AppearanceStrategy.SetFont(new LogViewer.FontData(ReadFontNameControl(), ReadFontSizeControl()));
            sampleLogViewerPresenter.AppearanceStrategy.SetColoring(ReadColoringModeControl());
            if (fullUpdate)
            {
                temporaryColorTheme.SetBrightness(ReadColoringPaletteControl());
                FillSampleMessagesCollection();
            }
        }

        LogFontSize ReadFontSizeControl()
        {
            return view.FontSizes
                .Where(p => p.Value == fontSizeControl.Value)
                .Select(p => p.Key)
                .FirstOrDefault(LogFontSize.Normal);
        }

        string ReadFontNameControl()
        {
            int selectedFont = view.GetSelectedValue(ViewControl.FontFamilySelector);
            var availableFonts = view.AvailablePreferredFamilies;
            return (selectedFont >= 0 && selectedFont < availableFonts.Length) ? availableFonts[selectedFont] : null;
        }

        ColoringMode ReadColoringModeControl()
        {
            return (ColoringMode)view.GetSelectedValue(ViewControl.ColoringSelector);
        }

        PaletteBrightness ReadColoringPaletteControl()
        {
            return (PaletteBrightness)view.GetSelectedValue(ViewControl.PaletteSelector);
        }

        class TemporaryColorTheme : IColorTheme
        {
            private readonly IColorTheme appTheme;
            private readonly IColorTable threadsColorTable;
            private readonly IChangeNotification changeNotification;
            private PaletteBrightness paletteBrightness = PaletteBrightness.Normal;

            public TemporaryColorTheme(IColorTheme appTheme, IChangeNotification changeNotification)
            {
                this.appTheme = appTheme;
                this.changeNotification = changeNotification;
                this.threadsColorTable = new LogThreadsColorsTable(this, () => paletteBrightness);
            }

            ColorThemeMode IColorTheme.Mode => appTheme.Mode;

            ImmutableArray<Color> IColorTheme.ThreadColors => threadsColorTable.Items;

            ImmutableArray<Color> IColorTheme.HighlightingColors => appTheme.HighlightingColors;

            public int ThreadColorsCount => threadsColorTable.Items.Length;

            public void SetBrightness(PaletteBrightness paletteBrightness)
            {
                this.paletteBrightness = paletteBrightness;
                this.changeNotification.Post();
            }
        };

        readonly string[] coloringModes = new[]
        {
            "White background",
            "Background color represents message thread", // todo: dark mode
			"Background color represents message log source",
        };
        readonly string[] coloringPalettes = new[]
        {
            "Dark",
            "Normal",
            "Bright",
        };

        readonly IGlobalSettingsAccessor settingsAccessor;
        readonly LogViewer.IPresenterInternal sampleLogViewerPresenter;
        readonly TemporaryColorTheme temporaryColorTheme;
        readonly IModelThreadsInternal sampleThreads;
        readonly LogViewer.DummyModel dummyModel;
        readonly DateTime sampleMessagesBaseTime;
        readonly LabeledStepperPresenter.IPresenterInternal fontSizeControl;
        IView view;
    };
};