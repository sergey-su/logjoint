using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.BookmarksManager
{
	public interface IPresenter
	{
		void ShowNextBookmark();
		void ShowPrevBookmark();
		bool NavigateToBookmark(IBookmark bmk,
			Predicate<IMessage> messageMatcherWhenNoHashIsSpecified = null, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
		void ToggleBookmark();
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		bool ShowDeleteConfirmationPopup(int nrOfBookmarks);
	};

	public interface IViewEvents
	{
		void OnToggleButtonClicked();
		void OnDeleteAllButtonClicked();
		void OnPrevBmkButtonClicked();
		void OnNextBmkButtonClicked();
		void OnAddBookmarkButtonClicked();
		void OnDeleteBookmarkButtonClicked();
	};
};