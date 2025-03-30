using System;

namespace LogJoint.UI.Presenters.InlineSearch
{
    public interface IPresenter
    {
        event EventHandler<SearchEventArgs> OnSearch;

        void Show(string initialSearchString, Func<HitCounts> hitCount = null);
        void Hide();
        IViewModel ViewModel { get; }
        bool IsVisible { get; }
    };

    public record class HitCounts(int? Current, int? Total);

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        QuickSearchTextBox.IViewModel QuickSearchTextBox { get; }
        bool IsVisible { get; }
        HitCounts HitCounts { get; }

        void OnPrevClicked();
        void OnNextClicked();
        void OnHideClicked();
    };

    public class SearchEventArgs : EventArgs
    {
        public string Query { get; internal set; }
        public bool Reverse { get; internal set; }
    };
};