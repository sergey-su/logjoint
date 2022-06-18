using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.BookmarksManager
{
	public interface IPresenter
	{
		void ShowNextBookmark();
		void ShowPrevBookmark();
		Task<bool> NavigateToBookmark(IBookmark bmk, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
		void ToggleBookmark();
	};

	public class ButtonState
	{
		public bool Enabled { get; internal set; }
		public string Tooltip { get; internal set; }
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		ButtonState AddButton { get; }
		ButtonState DeleteButton { get; }
		ButtonState DeleteAllButton { get; }
		void OnToggleButtonClicked();
		void OnDeleteAllButtonClicked();
		void OnPrevBmkButtonClicked();
		void OnNextBmkButtonClicked();
		void OnAddBookmarkButtonClicked();
		void OnDeleteBookmarkButtonClicked();
	};
};