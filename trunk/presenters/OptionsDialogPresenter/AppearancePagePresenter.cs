using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Settings;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;

namespace LogJoint.UI.Presenters.Options.Appearance
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			IView view)
		{
			this.model = model;
			this.view = view;
			this.settingsAccessor = model.GlobalSettings;

			this.sampleThreads = new ModelThreads();
			this.sampleLogViewerPresenter = new LogViewer.Presenter(
				new LogViewer.DummyModel(threads: sampleThreads, messages: CreateSampleMessagesCollection()), 
				view.PreviewLogView,
				null);
			this.sampleLogViewerPresenter.ShowTime = false;
			this.sampleLogViewerPresenter.ShowRawMessages = false;
			this.sampleLogViewerPresenter.DisabledUserInteractions =
				LogViewer.UserInteraction.FontResizing | 
				LogViewer.UserInteraction.RawViewSwitching | 
				LogViewer.UserInteraction.FramesNavigationMenu |
				LogViewer.UserInteraction.CopyMenu;
			this.sampleLogViewerPresenter.UpdateView();

			this.viewFonts = view.PreviewLogView;

			view.SetPresenter(this);

			InitView();
			UpdateSampleLogView();
		}

		bool IPresenter.Apply()
		{
			settingsAccessor.Appearance = new Settings.Appearance()
			{
				Coloring = ReadColoringModeControl(),
				FontFamily = ReadFontNameControl(),
				FontSize = ReadFontSizeControl()
			};
			return true;
		}

		void IViewEvents.OnRadioButtonChecked(ViewControl control)
		{
			UpdateSampleLogView();
		}

		void IViewEvents.OnSelectedFontChanged()
		{
			UpdateSampleLogView();
		}

		void IViewEvents.OnFontSizeValueChanged()
		{
			UpdateSampleLogView();
		}

		#region Implementation

		void InitView()
		{
			var appearance = settingsAccessor.Appearance;
			
			foreach (var modeCtrl in coloringModesControls)
				view.SetControlChecked(modeCtrl.Item2, modeCtrl.Item1 == appearance.Coloring);

			view.SetFontFamiliesControl(viewFonts.AvailablePreferredFamilies,
				viewFonts.AvailablePreferredFamilies.IndexOf(f => string.Compare(f, appearance.FontFamily ?? "", true) == 0).GetValueOrDefault(0));

			view.SetFontSizeControl(
				viewFonts.FontSizes
					.Select(p => p.Value)
					.ToArray(),
				viewFonts.FontSizes
					.Where(p => p.Key == appearance.FontSize)
					.Select(p => p.Value)
					.FirstOrDefault(viewFonts.FontSizes[0].Value));
		}

		IMessagesCollection CreateSampleMessagesCollection()
		{
			var ret = new MessagesContainers.RangesManagingCollection();
			ret.SetActiveRange(0, 10);
			using (var range = ret.GetNextRangeToFill())
			{
				DateTime now = DateTime.UtcNow;
				var t1 = sampleThreads.RegisterThread("thread1", null);
				var t2 = sampleThreads.RegisterThread("thread2", null);
				var t3 = sampleThreads.RegisterThread("thread3", null);
				range.Add(new Content(0, t1, new MessageTimestamp(now.AddSeconds(0)), new StringSlice("sample message 0"), SeverityFlag.Info), false);
				range.Add(new Content(1, t2, new MessageTimestamp(now.AddSeconds(1)), new StringSlice("sample message 1"), SeverityFlag.Info), false);
				range.Add(new Content(2, t1, new MessageTimestamp(now.AddSeconds(2)), new StringSlice("warning: sample message 2"), SeverityFlag.Warning), false);
				range.Add(new Content(3, t3, new MessageTimestamp(now.AddSeconds(3)), new StringSlice("sample message 3"), SeverityFlag.Info), false);
				range.Add(new Content(4, t2, new MessageTimestamp(now.AddSeconds(4)), new StringSlice("error: sample message 4"), SeverityFlag.Error), false);
				range.Add(new Content(5, t1, new MessageTimestamp(now.AddSeconds(5)), new StringSlice("sample message 5"), SeverityFlag.Info), false);
				range.Complete();
			}
			return ret;
		}

		void UpdateSampleLogView()
		{
			sampleLogViewerPresenter.FontName = ReadFontNameControl();
			sampleLogViewerPresenter.FontSize = ReadFontSizeControl();
			sampleLogViewerPresenter.Coloring = ReadColoringModeControl();
		}

		LogFontSize ReadFontSizeControl()
		{
			return viewFonts.FontSizes
				.Where(p => p.Value == view.GetFontSizeControlValue())
				.Select(p => p.Key)
				.FirstOrDefault(LogFontSize.Normal);
		}

		string ReadFontNameControl()
		{
			int selectedFont = view.GetSelectedFontFamily();
			var availableFonts = viewFonts.AvailablePreferredFamilies;
			return (selectedFont >= 0 && selectedFont < availableFonts.Length) ? availableFonts[selectedFont] : null;
		}

		ColoringMode ReadColoringModeControl()
		{
			return coloringModesControls.Where(c => view.GetControlChecked(c.Item2)).Select(c => c.Item1).FirstOrDefault(ColoringMode.None);
		}

		readonly Tuple<ColoringMode, ViewControl>[] coloringModesControls = new[]
		{
			Tuple.Create(ColoringMode.None, ViewControl.ColoringNoneRadioButton),
			Tuple.Create(ColoringMode.Threads, ViewControl.ColoringThreadsRadioButton),
			Tuple.Create(ColoringMode.Sources, ViewControl.ColoringSourcesRadioButton)
		};

		readonly IModel model;
		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;
		readonly LogViewer.IViewFonts viewFonts;
		readonly LogViewer.IPresenter sampleLogViewerPresenter;
		readonly IModelThreads sampleThreads;

		#endregion

	};
};