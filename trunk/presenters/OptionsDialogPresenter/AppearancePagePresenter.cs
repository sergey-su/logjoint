using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LogJoint.Settings;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;

namespace LogJoint.UI.Presenters.Options.Appearance
{
	public class Presenter : IPresenter, IViewEvents, IDisposable
	{
		public Presenter(
			Settings.IGlobalSettingsAccessor settings,
			IView view,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			IChangeNotification changeNotification,
			IColorTheme theme
		)
		{
			this.view = view;
			this.settingsAccessor = settings;

			this.sampleMessagesBaseTime = DateTime.UtcNow;
			this.temporaryColorTheme = new TemporaryColorTheme(theme, changeNotification);
			this.sampleThreads = new ModelThreads(new ColorLease(temporaryColorTheme.ThreadColorsCount));
			this.dummyModel = new LogViewer.DummyModel(threads: sampleThreads);
			this.sampleLogViewerPresenter = logViewerPresenterFactory.Create(
				dummyModel, view.PreviewLogView,
				createIsolatedPresenter: true,
				theme: temporaryColorTheme);
			this.sampleLogViewerPresenter.ShowTime = false;
			this.sampleLogViewerPresenter.ShowRawMessages = false;
			this.sampleLogViewerPresenter.DisabledUserInteractions =
				LogViewer.UserInteraction.FontResizing | 
				LogViewer.UserInteraction.RawViewSwitching | 
				LogViewer.UserInteraction.FramesNavigationMenu |
				LogViewer.UserInteraction.CopyMenu;

			this.viewFonts = view.PreviewLogView;

			this.fontSizeControl = new LabeledStepperPresenter.Presenter(view.FontSizeControlView);
			this.fontSizeControl.OnValueChanged += (sender, e) => UpdateSampleLogView(fullUpdate: false);

			view.SetPresenter(this);

			InitView();

			UpdateSampleLogView(fullUpdate: true);
		}

		void IDisposable.Dispose()
		{
			sampleLogViewerPresenter.Dispose();
		}

		bool IPresenter.Apply()
		{
			settingsAccessor.Appearance = new Settings.Appearance(
				coloring: ReadColoringModeControl(),
				fontFamily: ReadFontNameControl(),
				fontSize: ReadFontSizeControl(),
				coloringBrightness: ReadColoringPaletteControl()
			);
			return true;
		}

		void IViewEvents.OnSelectedValueChanged(ViewControl ctrl)
		{
			UpdateSampleLogView(fullUpdate: ctrl == ViewControl.PaletteSelector);
		}

		#region Implementation

		void InitView()
		{
			var appearance = settingsAccessor.Appearance;

			view.SetSelectorControl(ViewControl.ColoringSelector, coloringModes, (int)appearance.Coloring);

			view.SetSelectorControl(ViewControl.FontFamilySelector, viewFonts.AvailablePreferredFamilies,
				viewFonts.AvailablePreferredFamilies.IndexOf(f => string.Compare(f, appearance.FontFamily ?? "", true) == 0).GetValueOrDefault(0));

			fontSizeControl.AllowedValues =
				viewFonts.FontSizes
					.Select(p => p.Value)
					.ToArray();
			fontSizeControl.Value =
				viewFonts.FontSizes
					.Where(p => p.Key == appearance.FontSize)
					.Select(p => p.Value)
					.FirstOrDefault(viewFonts.FontSizes[0].Value);

			view.SetSelectorControl(ViewControl.PaletteSelector, coloringPalettes, (int)appearance.ColoringBrightness);
		}

		void FillSampleMessagesCollection()
		{
			foreach (var t in sampleThreads.Items.ToArray())
				t.Dispose();

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
			sampleLogViewerPresenter.FontName = ReadFontNameControl();
			sampleLogViewerPresenter.FontSize = ReadFontSizeControl();
			sampleLogViewerPresenter.Coloring = ReadColoringModeControl();
			if (fullUpdate)
			{
				temporaryColorTheme.SetBrightness(ReadColoringPaletteControl());
				FillSampleMessagesCollection();
			}
		}

		LogFontSize ReadFontSizeControl()
		{
			return viewFonts.FontSizes
				.Where(p => p.Value == fontSizeControl.Value)
				.Select(p => p.Key)
				.FirstOrDefault(LogFontSize.Normal);
		}

		string ReadFontNameControl()
		{
			int selectedFont = view.GetSelectedValue(ViewControl.FontFamilySelector);
			var availableFonts = viewFonts.AvailablePreferredFamilies;
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

			ImmutableArray<ModelColor> IColorTheme.ThreadColors => threadsColorTable.Items;

			ImmutableArray<ModelColor> IColorTheme.HighlightingColors => appTheme.HighlightingColors;

			public int ThreadColorsCount => threadsColorTable.Items.Length;

			public void SetBrightness(PaletteBrightness paletteBrightness)
			{
				this.paletteBrightness = paletteBrightness;
				this.changeNotification.Post();
			}
		};

		readonly string[] coloringModes = new[] 
		{
			"White backgound",
			"Background color represents message thread", // todo: dark mode
			"Background color represents message log source",
		};
		readonly string[] coloringPalettes = new[] 
		{
			"Dark",
			"Normal",
			"Bright",
		};

		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;
		readonly LogViewer.IViewFonts viewFonts;
		readonly LogViewer.IPresenter sampleLogViewerPresenter;
		readonly TemporaryColorTheme temporaryColorTheme;
		readonly IModelThreads sampleThreads;
		LogViewer.DummyModel dummyModel;
		readonly DateTime sampleMessagesBaseTime;
		readonly LabeledStepperPresenter.IPresenter fontSizeControl;

		#endregion

	};
};