using System;

namespace LogJoint.UI.Presenters.InlineSearch
{
	public interface IPresenter
	{
		event EventHandler<SearchEventArgs> OnSearch;

		void Show(string initialSearchString);
		void Hide();
		IViewModel ViewModel { get; }
		bool IsVisible { get; }
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		QuickSearchTextBox.IViewModel QuickSearchTextBox { get; }
		bool IsVisible { get; }

		void OnPrevClicked();
		void OnNextClicked();
		void OnHideClicked();
	};

	public class SearchEventArgs: EventArgs
	{
		public string Query { get; internal set; }
		public bool Reverse { get; internal set; } 
	};
};