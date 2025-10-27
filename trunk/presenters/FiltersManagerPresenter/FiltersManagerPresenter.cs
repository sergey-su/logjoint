using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint;
using System.Runtime.InteropServices;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.FiltersManager
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly IChangeNotification changeNotification;
        readonly Func<ViewControl> visibleCtrls;
        readonly Func<ViewControl> enabledCtrls;
        readonly IFiltersFactory filtersFactory;
        readonly FilterDialog.IPresenter filtersDialogPresenter;
        readonly FiltersListBox.IPresenter filtersListPresenter;
        readonly LogViewer.IPresenterInternal loadedMessagesLogViewerPresenter;
        readonly LogViewer.IPresenterInternal searchResultsLogViewerPresenter;
        readonly IAlertPopup alerts;
        int lastFilterIndex;
        IFiltersList filtersList;

        public Presenter(
            IChangeNotification changeNotification,
            FiltersListBox.IPresenter filtersListPresenter,
            FilterDialog.IPresenter filtersDialogPresenter,
            LogViewer.IPresenterInternal loadedMessagesLogViewerPresenter,
            LogViewer.IPresenterInternal searchResultsLogViewerPresenter,
            IFiltersFactory filtersFactory,
            IAlertPopup alerts,
            IFiltersList initialFiltersList = null
        )
        {
            this.changeNotification = changeNotification;
            this.filtersListPresenter = filtersListPresenter;
            this.filtersDialogPresenter = filtersDialogPresenter;
            this.loadedMessagesLogViewerPresenter = loadedMessagesLogViewerPresenter;
            this.searchResultsLogViewerPresenter = searchResultsLogViewerPresenter;
            this.filtersFactory = filtersFactory;
            this.alerts = alerts;

            this.visibleCtrls = Selectors.Create(() => filtersListPresenter.SelectedFilters,
                () => filtersList?.Purpose, GetVisibleCtrls);
            this.enabledCtrls = Selectors.Create(() => filtersListPresenter.SelectedFilters,
                () => filtersList?.Purpose, () => filtersList != null && filtersList.FilteringEnabled,
                () => filtersList?.Items, GetEnabledCtrls);

            filtersListPresenter.DeleteRequested += (s, a) =>
            {
                DoRemoveSelected();
            };

            if (initialFiltersList != null)
            {
                SetFiltersList(initialFiltersList);
            }
        }

        IFiltersList IPresenter.FiltersList
        {
            get { return filtersList; }
            set { SetFiltersList(value); }
        }

        void IPresenter.OpenDialogForSelectedText()
        {
            OpenDialog(requireNonEmptyText: true, preferEditExistingFilter: true);
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        FiltersListBox.IViewModel IViewModel.FiltersListBox => (FiltersListBox.IViewModel)filtersListPresenter;

        FilterDialog.IViewModel IViewModel.FilterDialog => (FilterDialog.IViewModel)filtersDialogPresenter;

        ViewControl IViewModel.VisibileControls => visibleCtrls();

        ViewControl IViewModel.EnabledControls => enabledCtrls();

        (bool isChecked, string tooltip, string label) IViewModel.FiltertingEnabledCheckBox
        {
            get
            {
                if (filtersList == null)
                {
                    return (false, "", "");
                }
                if (filtersList.Purpose == FiltersListPurpose.Highlighting)
                {
                    return (
                        filtersList.FilteringEnabled,
                        filtersList.FilteringEnabled ?
                            "Unckeck to disable all highlighting temporarily" : "Check to enable highlighting",
                        "Enabled highlighting"
                    );
                }
                else if (filtersList.Purpose == FiltersListPurpose.Display)
                {
                    return (
                        filtersList.FilteringEnabled,
                        filtersList.FilteringEnabled ?
                            "Unckeck to disable all filtering temporarily" : "Check to enable filtering",
                        "Enabled filtering"
                    );
                }
                else
                {
                    return (
                        false,
                        "",
                        "Enable filtering"
                    );
                }
            }
        }

        void IViewModel.OnEnableFilteringChecked(bool value)
        {
            if (filtersList != null)
                filtersList.FilteringEnabled = value;
        }

        void IViewModel.OnAddFilterClicked()
        {
            OpenDialog(requireNonEmptyText: false, preferEditExistingFilter: false);
        }

        void IViewModel.OnRemoveFilterClicked()
        {
            DoRemoveSelected();
        }

        void IViewModel.OnMoveFilterUpClicked()
        {
            MoveFilterInternal(up: true);
        }

        void IViewModel.OnMoveFilterDownClicked()
        {
            MoveFilterInternal(up: false);
        }

        void IViewModel.OnPrevClicked()
        {
            loadedMessagesLogViewerPresenter?.GoToPrevHighlightedMessage();
        }

        void IViewModel.OnNextClicked()
        {
            loadedMessagesLogViewerPresenter?.GoToNextHighlightedMessage();
        }

        async void IViewModel.OnOptionsClicked()
        {
            var f = filtersListPresenter.SelectedFilters.FirstOrDefault();
            if (f != null)
                await filtersDialogPresenter.ShowTheDialog(f, filtersList.Purpose);
        }

        void MoveFilterInternal(bool up)
        {
            foreach (var f in filtersListPresenter.SelectedFilters)
            {
                filtersList.Move(f, up);
                break;
            }
        }

        static ViewControl GetVisibleCtrls(IImmutableSet<IFilter> selectedFilters, FiltersListPurpose? purpose)
        {
            ViewControl visibleCtrls =
                ViewControl.AddFilterButton | ViewControl.RemoveFilterButton |
                ViewControl.MoveUpButton | ViewControl.MoveDownButton | ViewControl.FilterOptions;
            if (purpose == FiltersListPurpose.Highlighting)
                visibleCtrls |= (ViewControl.FilteringEnabledCheckbox | ViewControl.PrevButton | ViewControl.NextButton);
            else if (purpose == FiltersListPurpose.Display)
                visibleCtrls |= ViewControl.FilteringEnabledCheckbox;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (purpose == FiltersListPurpose.Highlighting)
                    visibleCtrls &= ~(ViewControl.MoveUpButton | ViewControl.MoveDownButton);
            }
            return visibleCtrls;
        }

        static ViewControl GetEnabledCtrls(IImmutableSet<IFilter> selectedFilters, FiltersListPurpose? purpose,
            bool filteringEnabled, IImmutableList<IFilter> allFilters)
        {
            int count = selectedFilters.Count;
            ViewControl enabledCtrls =
                ViewControl.FilteringEnabledCheckbox | ViewControl.AddFilterButton;
            if (count > 0)
                enabledCtrls |= ViewControl.RemoveFilterButton;
            if (count == 1)
                enabledCtrls |= ViewControl.FilterOptions;
            if (count == 1 && allFilters != null && allFilters.Count > 1)
            {
                if (selectedFilters.Single() != allFilters[0])
                    enabledCtrls |= ViewControl.MoveUpButton;
                if (selectedFilters.Single() != allFilters[allFilters.Count - 1])
                    enabledCtrls |= ViewControl.MoveDownButton;
            }
            if (purpose == FiltersListPurpose.Highlighting && IsNavigationOverHighlightedMessagesEnabled(selectedFilters.Count, filteringEnabled))
                enabledCtrls |= (ViewControl.PrevButton | ViewControl.NextButton);
            return enabledCtrls;
        }

        static bool IsNavigationOverHighlightedMessagesEnabled(int filtersCount, bool filteringEnabled)
        {
            return filteringEnabled && filtersCount > 0;
        }

        void SetFiltersList(IFiltersList value)
        {
            filtersList = value;
            filtersListPresenter.FiltersList = value;
            changeNotification.Post();
        }

        private async void DoRemoveSelected()
        {
            var toDelete = new List<IFilter>();
            foreach (IFilter f in filtersListPresenter.SelectedFilters)
            {
                toDelete.Add(f);
            }

            if (toDelete.Count == 0)
            {
                return;
            }

            if (await alerts.ShowPopupAsync(
                "Rules",
                string.Format("You are about to delete ({0}) rules(s).\nAre you sure?", toDelete.Count),
                AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) != AlertFlags.Yes)
            {
                return;
            }

            filtersList.Delete(toDelete);
        }

        private async void OpenDialog(bool requireNonEmptyText, bool preferEditExistingFilter)
        {
            string defaultTemplate = "";
            string selectedText = "";
            LogViewer.IPresenterInternal logViewerPresenter = 
                new[] { searchResultsLogViewerPresenter, loadedMessagesLogViewerPresenter }
                    .Where(p => p != null)
                    .OrderByDescending(p => (p.HasInputFocus ? 1 : 0, p.LastFocusTime))
                    .FirstOrDefault();
            if (logViewerPresenter != null)
            {
                selectedText = await logViewerPresenter.GetSelectedText().IgnoreCancellation(s => s, "");
            }
            if (selectedText.Split(['\r', '\n']).Length < 2) // is single-line
                defaultTemplate = selectedText;
            if (requireNonEmptyText && defaultTemplate.Length == 0)
            {
                return;
            }
            IFilter existingFilter = null;
            if (preferEditExistingFilter)
            {
                existingFilter = filtersList.Items.FirstOrDefault(candidate => !candidate.IsDisposed 
                    && candidate.Options.Template == defaultTemplate);
            }
            IFilter newFilter = null;
            if (existingFilter == null)
            {
                newFilter = filtersFactory.CreateFilter(
                    filtersList.Purpose switch
                    {
                        FiltersListPurpose.Highlighting => GetNextHighlightingFilterAction(filtersList),
                        FiltersListPurpose.Display => FilterAction.Exclude,
                        _ => FilterAction.Include
                    },
                    string.Format("New rule {0}", ++lastFilterIndex),
                    enabled: true,
                    searchOptions: new Search.Options()
                    {
                        Template = defaultTemplate,
                        Scope = filtersFactory.CreateScope()
                    },
                    timeRange: null
                );
            }
            try
            {
                if (!await filtersDialogPresenter.ShowTheDialog(existingFilter ?? newFilter, filtersList.Purpose))
                {
                    return;
                }
                if (newFilter != null)
                {
                    filtersList.Insert(0, newFilter);
                }
                newFilter = null;
            }
            finally
            {
                newFilter?.Dispose();
            }
        }

        static private FilterAction GetNextHighlightingFilterAction(IFiltersList filtersList)
        {
            var usedActions = filtersList.Items.Select(f => f.Action).ToHashSet();
            for (FilterAction action = FilterAction.IncludeAndColorizeFirst;; ++action)
            {
                if (!usedActions.Contains(action))
                {
                    return action;
                }
                if (action == FilterAction.IncludeAndColorizeLast)
                {
                    break;
                }
            }
            return FilterAction.IncludeAndColorize1;
        }
    };
};