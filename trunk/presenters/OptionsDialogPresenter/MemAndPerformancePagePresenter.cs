using System;
using LogJoint.Settings;
using LogJoint.MRU;

namespace LogJoint.UI.Presenters.Options.MemAndPerformancePage
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			Settings.IGlobalSettingsAccessor settings,
			IRecentlyUsedEntities mru,
			ISearchHistory searchHistory,
			IView view)
		{
			this.view = view;
			this.settingsAccessor = settings;
			this.recentLogsList = mru;
			this.searchHistory = searchHistory;
			this.recentLogsListSizeEditor = new LabeledStepperPresenter.Presenter(view.GetStepperView(ViewControl.RecentLogsListSizeEditor));
			this.searchHistoryDepthEditor = new LabeledStepperPresenter.Presenter(view.GetStepperView(ViewControl.SearchHistoryDepthEditor));
			this.maxNumberOfSearchResultsEditor = new LabeledStepperPresenter.Presenter(view.GetStepperView(ViewControl.MaxNumberOfSearchResultsEditor));
			this.logSizeThresholdEditor = new LabeledStepperPresenter.Presenter(view.GetStepperView(ViewControl.LogSizeThresholdEditor));
			this.logWindowSizeEditor = new LabeledStepperPresenter.Presenter(view.GetStepperView(ViewControl.LogWindowSizeEditor));

			this.logSizeThresholdEditor.AllowedValues = new int[] {
				1,
				2,
				4,
				8,
				12,
				16,
				24,
				32,
				48,
				64,
				80,
				100,
				120,
				160,
				200
			};
			this.logSizeThresholdEditor.Value = 1;

			this.logWindowSizeEditor.AllowedValues = new int[] {
				1,
				2,
				3,
				4,
				5,
				6,
				8,
				12,
				20,
				24
			};
			this.logWindowSizeEditor.Value = 1;

			this.searchHistoryDepthEditor.AllowedValues = new int[] {
				0,
				5,
				10,
				20,
				30,
				40,
				50,
				60,
				70,
				80,
				90,
				100,
				120,
				140,
				160,
				180,
				200,
				220,
				240,
				260,
				280,
				300};
			this.searchHistoryDepthEditor.Value = 0;

			this.maxNumberOfSearchResultsEditor.AllowedValues = new int[] {
				1000,
				4000,
				8000,
				16000,
				30000,
				50000,
				70000,
				100000,
				200000};
			this.maxNumberOfSearchResultsEditor.Value = 1000;


			this.recentLogsListSizeEditor.AllowedValues = new int[] {
				0,
				100,
				200,
				400,
				800,
				1500};
			this.recentLogsListSizeEditor.Value = 0;

			view.SetPresenter(this);

			UpdateView();
		}

		bool IPresenter.Apply()
		{
			recentLogsList.RecentEntriesListSizeLimit = recentLogsListSizeEditor.Value;
			searchHistory.MaxCount = searchHistoryDepthEditor.Value;
			settingsAccessor.MultithreadedParsingDisabled = view.GetControlChecked(ViewControl.DisableMultithreadedParsingCheckBox);
			settingsAccessor.MaxNumberOfHitsInSearchResultsView = maxNumberOfSearchResultsEditor.Value;
			settingsAccessor.FileSizes = new FileSizes()
			{
				Threshold = logSizeThresholdEditor.Value,
				WindowSize = logWindowSizeEditor.Value
			};
			settingsAccessor.EnableAutoPostprocessing = view.GetControlChecked(ViewControl.EnableAutoPostprocessingCheckBox);
			return true;
		}

		void IViewEvents.OnLinkClicked(ViewControl control)
		{
			if (control == ViewControl.ClearRecentEntriesListLinkLabel)
			{
				if (view.ShowConfirmationDialog("Are you sure you want to clear recent logs history?"))
				{
					recentLogsList.ClearRecentLogsList();
					UpdateRecentLogsControls();
				}
			}
			else if (control == ViewControl.ClearSearchHistoryLinkLabel)
			{
				if (view.ShowConfirmationDialog("Are you sure you want to clear current queries history?"))
				{
					searchHistory.Clear();
					UpdateSearchHistoryControls();
				}
			}
			else if (control == ViewControl.CollectUnusedMemoryLinkLabel)
			{
				GC.Collect(2, GCCollectionMode.Forced);
				GC.WaitForPendingFinalizers();
				GC.Collect(2, GCCollectionMode.Forced);
				UpdateMemoryConsumptionLink();
			}
		}

		void IViewEvents.OnCheckboxChecked(ViewControl control)
		{
		}

		#region Implementation

		void UpdateView()
		{
			UpdateRecentLogsControls();
			UpdateSearchHistoryControls();
			UpdateSearchResultsLimitsControls();
			UpdateFileSizesControls();
			UpdateMultithreadingControls();
			UpdateMemoryConsumptionLink();
			UpdatePostprocessingControls();
		}

		private void UpdateMultithreadingControls()
		{
			view.SetControlChecked(ViewControl.DisableMultithreadedParsingCheckBox, settingsAccessor.MultithreadedParsingDisabled);
		}

		private void UpdateSearchResultsLimitsControls()
		{
			maxNumberOfSearchResultsEditor.Value = settingsAccessor.MaxNumberOfHitsInSearchResultsView;
		}

		private void UpdateFileSizesControls()
		{
			var fileSizes = settingsAccessor.FileSizes;
			logSizeThresholdEditor.Value = fileSizes.Threshold;
			logWindowSizeEditor.Value = fileSizes.WindowSize;
		}

		private void UpdateSearchHistoryControls()
		{
			var searchHistorySize = searchHistory.Count;
			searchHistoryDepthEditor.Value = searchHistory.MaxCount;
			view.SetControlText(ViewControl.ClearSearchHistoryLinkLabel,
				string.Format("clear current history ({0} entries)", searchHistorySize));
			view.SetControlEnabled(ViewControl.ClearSearchHistoryLinkLabel, searchHistorySize > 0);
		}

		private void UpdateRecentLogsControls()
		{
			var currentRecentEntriesListSize = recentLogsList.MRUList.Count;
			recentLogsListSizeEditor.Value = recentLogsList.RecentEntriesListSizeLimit;
			view.SetControlText(ViewControl.ClearRecentEntriesListLinkLabel,
				string.Format("clear current history ({0} entries)", currentRecentEntriesListSize));
			view.SetControlEnabled(ViewControl.ClearRecentEntriesListLinkLabel, currentRecentEntriesListSize > 0);
		}

		private void UpdateMemoryConsumptionLink()
		{
			view.SetControlText(ViewControl.MemoryConsumptionLabel, StringUtils.FormatBytesUserFriendly(GC.GetTotalMemory(false)));
		}

		private void UpdatePostprocessingControls()
		{
			view.SetControlChecked(ViewControl.EnableAutoPostprocessingCheckBox, settingsAccessor.EnableAutoPostprocessing);
		}

		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;
		readonly IRecentlyUsedEntities recentLogsList;
		readonly ISearchHistory searchHistory;
		readonly LabeledStepperPresenter.IPresenter recentLogsListSizeEditor, searchHistoryDepthEditor, 
			maxNumberOfSearchResultsEditor, logSizeThresholdEditor, logWindowSizeEditor;

		#endregion
	};
};