using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	[Flags]
	public enum BookmarkNavigationOptions
	{
		Default = 0,
		EnablePopups = 1,
		GenericStringsSet = 2,
		BookmarksStringsSet = 4,
		SearchResultStringsSet = 8,
		NoLinksInPopups = 16,
	};

	/// <summary>
	/// A facade interface that aggregates main functions of few other presenters.
	/// </summary>
	public interface IPresentersFacade
	{
		bool ShowMessage(IBookmark bmk, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified = null);
		void ShowThread(IThread thread);
		void ShowLogSource(ILogSource source);
		void ShowMessageProperties();
		void ShowFiltersView();
		void SaveLogSourceAs(ILogSource logSource);
		void ExecuteThreadPropertiesDialog(IThread thread);
	};
};