using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
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

	public interface IViewModel
	{
		void OnToggleButtonClicked();
		void OnDeleteAllButtonClicked();
		void OnPrevBmkButtonClicked();
		void OnNextBmkButtonClicked();
		void OnAddBookmarkButtonClicked();
		void OnDeleteBookmarkButtonClicked();
	};
};