using System;
using LogJoint.Settings;
using LogJoint.MRU;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Options.MemAndPerformancePage
{
    internal class Presenter : IPresenter, IViewModel, IPresenterInternal
    {
        public Presenter(
            Settings.IGlobalSettingsAccessor settings,
            IRecentlyUsedEntities mru,
            ISearchHistory searchHistory,
            IChangeNotification changeNotification,
            IAlertPopup alertPopup
        )
        {
            this.settingsAccessor = settings;
            this.recentLogsList = mru;
            this.searchHistory = searchHistory;
            this.changeNotification = changeNotification;
            this.alertPopup = alertPopup;

            this.recentLogsListSizeEditor = new LabeledStepperPresenter.Presenter(changeNotification);
            this.searchHistoryDepthEditor = new LabeledStepperPresenter.Presenter(changeNotification);
            this.maxNumberOfSearchResultsEditor = new LabeledStepperPresenter.Presenter(changeNotification);
            this.logSizeThresholdEditor = new LabeledStepperPresenter.Presenter(changeNotification);
            this.logWindowSizeEditor = new LabeledStepperPresenter.Presenter(changeNotification);

            this.logSizeThresholdEditor.AllowedValues = [
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
            ];
            this.logSizeThresholdEditor.Value = 1;

            this.logWindowSizeEditor.AllowedValues = [
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
            ];
            this.logWindowSizeEditor.Value = 1;

            this.searchHistoryDepthEditor.AllowedValues = [
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
                300];
            this.searchHistoryDepthEditor.Value = 0;

            this.maxNumberOfSearchResultsEditor.AllowedValues = [
                1000,
                4000,
                8000,
                16000,
                30000,
                50000,
                70000,
                100000,
                200000];
            this.maxNumberOfSearchResultsEditor.Value = 1000;


            this.recentLogsListSizeEditor.AllowedValues = [
                0,
                100,
                200,
                400,
                800,
                1500];
            this.recentLogsListSizeEditor.Value = 0;
        }

        void IPresenter.Load()
        {
            multithreadedParsingDisabled = settingsAccessor.MultithreadedParsingDisabled;
            enableAutoPostprocessing = settingsAccessor.EnableAutoPostprocessing;

            recentLogsListSizeEditor.Value = recentLogsList.RecentEntriesListSizeLimit;
            maxNumberOfSearchResultsEditor.Value = settingsAccessor.MaxNumberOfHitsInSearchResultsView;

            var fileSizes = settingsAccessor.FileSizes;
            logSizeThresholdEditor.Value = fileSizes.Threshold;
            logWindowSizeEditor.Value = fileSizes.WindowSize;

            searchHistoryDepthEditor.Value = searchHistory.MaxCount;

            UpdateView();
        }

        bool IPresenter.Apply()
        {
            recentLogsList.RecentEntriesListSizeLimit = recentLogsListSizeEditor.Value;
            searchHistory.MaxCount = searchHistoryDepthEditor.Value;
            settingsAccessor.MultithreadedParsingDisabled = multithreadedParsingDisabled;
            settingsAccessor.MaxNumberOfHitsInSearchResultsView = maxNumberOfSearchResultsEditor.Value;
            settingsAccessor.FileSizes = new FileSizes()
            {
                Threshold = logSizeThresholdEditor.Value,
                WindowSize = logWindowSizeEditor.Value
            };
            settingsAccessor.EnableAutoPostprocessing = enableAutoPostprocessing;
            return true;
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        IReadOnlyDictionary<ViewControl, ViewControlState> IViewModel.Controls => controls;

        LabeledStepperPresenter.IViewModel IViewModel.RecentLogsListSizeEditor => recentLogsListSizeEditor;

        LabeledStepperPresenter.IViewModel IViewModel.SearchHistoryDepthEditor => searchHistoryDepthEditor;

        LabeledStepperPresenter.IViewModel IViewModel.MaxNumberOfSearchResultsEditor => maxNumberOfSearchResultsEditor;

        LabeledStepperPresenter.IViewModel IViewModel.LogSizeThresholdEditor => logSizeThresholdEditor;

        LabeledStepperPresenter.IViewModel IViewModel.LogWindowSizeEditor => logWindowSizeEditor;

        async void IViewModel.OnLinkClicked(ViewControl control)
        {
            if (control == ViewControl.ClearRecentEntriesListLinkLabel)
            {
                if (await alertPopup.ShowPopupAsync("Confirmation",
                    "Are you sure you want to clear recent logs history?",
                    AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) == AlertFlags.Yes)
                {
                    recentLogsList.ClearRecentLogsList();
                    UpdateView();
                }
            }
            else if (control == ViewControl.ClearSearchHistoryLinkLabel)
            {
                if (await alertPopup.ShowPopupAsync("Confirmation",
                    "Are you sure you want to clear current queries history?",
                    AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) == AlertFlags.Yes)
                {
                    searchHistory.Clear();
                    UpdateView();
                }
            }
            else if (control == ViewControl.CollectUnusedMemoryLinkLabel)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
                UpdateView();
            }
        }

        void IViewModel.OnCheckboxChecked(ViewControl control, bool value)
        {
            if (control == ViewControl.EnableAutoPostprocessingCheckBox)
            {
                enableAutoPostprocessing = value;
                UpdateView();
            }
            else if (control == ViewControl.DisableMultithreadedParsingCheckBox)
            {
                multithreadedParsingDisabled = value;
                UpdateView();
            }
        }

        void UpdateView()
        {
            var builder = ImmutableDictionary.CreateBuilder<ViewControl, ViewControlState>();
            UpdateRecentLogsControls(builder);
            UpdateSearchHistoryControls(builder);
            UpdateMultithreadingControls(builder);
            UpdateMemoryConsumptionLink(builder);
            UpdatePostprocessingControls(builder);
            controls = builder.ToImmutable();
            changeNotification.Post();
        }

        private void UpdateMultithreadingControls(ImmutableDictionary<ViewControl, ViewControlState>.Builder controls)
        {
            controls[ViewControl.DisableMultithreadedParsingCheckBox] = new ViewControlState()
            {
                Enabled = true,
                Text = "Disable multi-threaded log parsing",
                Checked = multithreadedParsingDisabled,
            };
        }

        private void UpdateSearchHistoryControls(ImmutableDictionary<ViewControl, ViewControlState>.Builder controls)
        {
            var searchHistorySize = searchHistory.Count;
            controls[ViewControl.ClearSearchHistoryLinkLabel] = new ViewControlState()
            {
                Text = string.Format("clear current history ({0} entries)", searchHistorySize),
                Enabled = searchHistorySize > 0,
            };
        }

        private void UpdateRecentLogsControls(ImmutableDictionary<ViewControl, ViewControlState>.Builder controls)
        {
            var currentRecentEntriesListSize = recentLogsList.MRUList.Count;
            controls[ViewControl.ClearRecentEntriesListLinkLabel] = new ViewControlState()
            {
                Text = string.Format("clear current history ({0} entries)", currentRecentEntriesListSize),
                Enabled = currentRecentEntriesListSize > 0
            };
        }

        private void UpdateMemoryConsumptionLink(ImmutableDictionary<ViewControl, ViewControlState>.Builder controls)
        {
            controls[ViewControl.MemoryConsumptionLabel] = new ViewControlState()
            {
                Enabled = true,
                Text = StringUtils.FormatBytesUserFriendly(GC.GetTotalMemory(false))
            };
        }

        private void UpdatePostprocessingControls(ImmutableDictionary<ViewControl, ViewControlState>.Builder controls)
        {
            controls[ViewControl.EnableAutoPostprocessingCheckBox] = new ViewControlState()
            { 
                Enabled = true,
                Text = "Enable automatic logs postprocessing",
                Checked = enableAutoPostprocessing
            };
        }

        readonly IChangeNotification changeNotification;
        readonly IGlobalSettingsAccessor settingsAccessor;
        readonly IRecentlyUsedEntities recentLogsList;
        readonly ISearchHistory searchHistory;
        readonly IAlertPopup alertPopup;
        readonly LabeledStepperPresenter.IPresenterInternal recentLogsListSizeEditor, searchHistoryDepthEditor,
            maxNumberOfSearchResultsEditor, logSizeThresholdEditor, logWindowSizeEditor;
        IReadOnlyDictionary<ViewControl, ViewControlState> controls = ImmutableDictionary<ViewControl, ViewControlState>.Empty;
        bool multithreadedParsingDisabled, enableAutoPostprocessing;
    };
};