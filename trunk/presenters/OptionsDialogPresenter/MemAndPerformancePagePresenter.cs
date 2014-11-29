using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.Options.MemAndPerformancePage
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
			this.recentLogsList = model.MRU;
			this.searchHistory = model.SearchHistory;

			view.SetPresenter(this);

			UpdateView();
		}

		bool IPresenter.Apply()
		{
			recentLogsList.RecentLogsListSizeLimit = view.GetControlValue(ViewControl.RecentLogsListSizeEditor);
			searchHistory.MaxCount = view.GetControlValue(ViewControl.SearchHistoryDepthEditor);
			settingsAccessor.MultithreadedParsingDisabled = view.GetControlChecked(ViewControl.DisableMultithreadedParsingCheckBox);
			settingsAccessor.MaxNumberOfHitsInSearchResultsView = view.GetControlValue(ViewControl.MaxNumberOfSearchResultsEditor);
			settingsAccessor.FileSizes = new FileSizes()
			{
				Threshold = view.GetControlValue(ViewControl.LogSizeThresholdEditor),
				WindowSize = view.GetControlValue(ViewControl.LogWindowSizeEditor)
			};
			return true;
		}

		void IViewEvents.OnLinkClicked(ViewControl control)
		{
			if (control == ViewControl.ClearRecentLogsListLinkLabel)
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
		}

		private void UpdateMultithreadingControls()
		{
			view.SetControlChecked(ViewControl.DisableMultithreadedParsingCheckBox, settingsAccessor.MultithreadedParsingDisabled);
		}

		private void UpdateSearchResultsLimitsControls()
		{
			view.SetControlValue(ViewControl.MaxNumberOfSearchResultsEditor, settingsAccessor.MaxNumberOfHitsInSearchResultsView);
		}

		private void UpdateFileSizesControls()
		{
			var fileSizes = settingsAccessor.FileSizes;
			view.SetControlValue(ViewControl.LogSizeThresholdEditor, fileSizes.Threshold);
			view.SetControlValue(ViewControl.LogWindowSizeEditor, fileSizes.WindowSize);
		}

		private void UpdateSearchHistoryControls()
		{
			var searchHistorySize = searchHistory.Count;
			view.SetControlValue(ViewControl.SearchHistoryDepthEditor, searchHistory.MaxCount);
			view.SetControlText(ViewControl.ClearSearchHistoryLinkLabel,
				string.Format("clear current history ({0} entries)", searchHistorySize));
			view.SetControlEnabled(ViewControl.ClearSearchHistoryLinkLabel, searchHistorySize > 0);
		}

		private void UpdateRecentLogsControls()
		{
			var currentRecentLogsListSize = recentLogsList.GetMRUList().Count();
			view.SetControlValue(ViewControl.RecentLogsListSizeEditor, recentLogsList.RecentLogsListSizeLimit);
			view.SetControlText(ViewControl.ClearRecentLogsListLinkLabel,
				string.Format("clear current history ({0} entries)", currentRecentLogsListSize));
			view.SetControlEnabled(ViewControl.ClearRecentLogsListLinkLabel, currentRecentLogsListSize > 0);
		}

		private void UpdateMemoryConsumptionLink()
		{
			view.SetControlText(ViewControl.MemoryConsumptionLabel, StringUtils.FormatBytesUserFriendly(GC.GetTotalMemory(false)));
		}

		readonly IModel model;
		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;
		readonly IRecentlyUsedLogs recentLogsList;
		readonly ISearchHistory searchHistory;

		#endregion
	};
};