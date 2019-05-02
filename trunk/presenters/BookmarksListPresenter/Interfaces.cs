using System;
using System.Collections.Generic;
using LogJoint.Settings;

namespace LogJoint.UI.Presenters.BookmarksList
{
	public delegate void BookmarkEvent(IPresenter sender, IBookmark bmk);

	public interface IPresenter
	{
		event BookmarkEvent Click;
		void DeleteSelectedBookmarks();
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
	};

	public struct ViewItem
	{
		public string Delta, AltDelta;
		public bool IsSelected;
		public bool IsEnabled;
		public string Text;
		public ModelColor? ContextColor;

		internal IBookmark Bookmark;
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		string FontName { get; }
		ColorThemeMode Theme { get; }
		IReadOnlyList<ViewItem> Items { get; }
		Tuple<int, int> FocusedMessagePosition { get; }

		void OnEnterKeyPressed();
		void OnViewDoubleClicked();
		void OnBookmarkLeftClicked(ViewItem item);
		void OnMenuItemClicked(ContextMenuItem item);
		ContextMenuItem OnContextMenu();
		void OnCopyShortcutPressed();
		void OnDeleteButtonPressed();
		void OnSelectAllShortcutPressed();
		void OnChangeSelection(IEnumerable<ViewItem> selected);
	};

	[Flags]
	public enum ContextMenuItem
	{
		None = 0,
		Delete = 1,
		Copy = 2,
		CopyWithDeltas = 4
	};

	[Flags]
	public enum ViewUpdateFlags
	{
		None = 0,
		SelectionDidNotChange = 1,
		ItemsCountDidNotChange = 2
	};
};