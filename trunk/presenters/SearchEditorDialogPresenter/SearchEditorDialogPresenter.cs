using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchEditorDialog
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly IChangeNotification changeNotification;
        readonly IUserDefinedSearches userDefinedSearches;
        readonly IAlertPopup alerts;
        readonly FiltersManager.IPresenter filtersPresenter;
        readonly FiltersManager.IViewModel filtersViewModel;
        const string alertsCaption = "Filter editor";

        TaskCompletionSource<bool> currentResult;
        IFiltersList tempList;
        string tempName;
        IUserDefinedSearch currentSearch;


        public Presenter(
            IChangeNotification changeNotification,
            IUserDefinedSearches userDefinedSearches,
            FiltersManager.IPresenter filtersManagerPresenter,
            IAlertPopup alerts
        )
        {
            this.changeNotification = changeNotification;
            this.userDefinedSearches = userDefinedSearches;
            this.filtersViewModel = (FiltersManager.IViewModel)filtersManagerPresenter;
            this.filtersPresenter = filtersManagerPresenter;
            this.alerts = alerts;
        }

        Task<bool> IPresenter.Open(IUserDefinedSearch search)
        {
            Reset();
            currentSearch = search;
            currentResult = new TaskCompletionSource<bool>();
            tempList = search.Filters.Clone();
            tempName = search.Name;
            filtersPresenter.FiltersList = tempList;
            changeNotification.Post();
            return currentResult.Task;
        }

        FiltersManager.IViewModel IViewModel.FiltersManager => filtersViewModel;

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        bool IViewModel.IsVisible => currentResult != null;

        string IViewModel.Name => tempName;

        async void IViewModel.OnConfirmed()
        {
            if (string.IsNullOrWhiteSpace(tempName))
            {
                await alerts.ShowPopupAsync(
                    alertsCaption,
                    "Bad filter name.",
                    AlertFlags.Ok
                );
                return;
            }
            if (tempName != currentSearch.Name && userDefinedSearches.ContainsItem(tempName))
            {
                await alerts.ShowPopupAsync(
                    alertsCaption,
                    string.Format("Name '{0}' is already used by another filter. Enter another name.", tempName),
                    AlertFlags.Ok);
                return;
            }
            if (tempList.Items.Count == 0)
            {
                await alerts.ShowPopupAsync(
                    alertsCaption,
                    "Can not save: filter must have at least one rule.",
                    AlertFlags.Ok);
                return;
            }
            currentSearch.Name = tempName;
            currentSearch.Filters = tempList;
            tempList = null;
            currentResult.TrySetResult(true);
            currentResult = null;
            changeNotification.Post();
        }

        void IViewModel.OnCancelled()
        {
            Reset();
        }

        void IViewModel.OnChangeName(string name)
        {
            tempName = name;
            changeNotification.Post();
        }

        void Reset()
        {
            if (currentResult != null)
            {
                currentResult.TrySetResult(false);
                currentResult = null;
                changeNotification.Post();
            }
            if (tempList != null)
            {
                tempList.Dispose();
                tempList = null;
            }
            filtersPresenter.FiltersList = null;
        }
    };
}