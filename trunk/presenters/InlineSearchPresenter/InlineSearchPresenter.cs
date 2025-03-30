using System;

namespace LogJoint.UI.Presenters.InlineSearch
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly IChangeNotification changeNotification;
        readonly QuickSearchTextBox.IPresenter searchBox;
        bool isVisible;
        Func<HitCounts> hitCount;

        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
            this.searchBox = new QuickSearchTextBox.Presenter(null, changeNotification);
            searchBox.HideClearButton();

            searchBox.OnCancelled += (s, e) => DoHide();
            searchBox.OnSearchNow += (s, e) => DoSearch(e.ReverseSearchModifier);
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        QuickSearchTextBox.IViewModel IViewModel.QuickSearchTextBox => searchBox.ViewModel;

        bool IViewModel.IsVisible => isVisible;

        HitCounts IViewModel.HitCounts => hitCount != null ? hitCount() : null;

        public event EventHandler<SearchEventArgs> OnSearch;

        void IPresenter.Hide() => DoHide();

        bool IPresenter.IsVisible => isVisible;

        IViewModel IPresenter.ViewModel => this;

        void IViewModel.OnHideClicked() => DoHide();

        void IViewModel.OnNextClicked() => DoSearch(reverse: false);

        void IViewModel.OnPrevClicked() => DoSearch(reverse: true);

        void IPresenter.Show(string initialSearchString, Func<HitCounts> hitCount)
        {
            if (!isVisible)
            {
                isVisible = true;
                this.hitCount = hitCount;
                changeNotification.Post();
            }
            searchBox.Focus(initialSearchString);
        }

        void DoHide()
        {
            if (isVisible)
            {
                isVisible = false;
                hitCount = null;
                changeNotification.Post();
            }
        }

        void DoSearch(bool reverse)
        {
            if (searchBox.Text != "")
            {
                OnSearch?.Invoke(this, new SearchEventArgs()
                {
                    Query = searchBox.Text,
                    Reverse = reverse
                });
            }
        }
    };
};