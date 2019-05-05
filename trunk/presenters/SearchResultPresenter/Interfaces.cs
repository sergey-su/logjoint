using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchResult
{
	public interface IView
	{
		void SetViewModel(IViewModel presenter);
		Presenters.LogViewer.IView MessagesView { get; }
		void UpdateExpandedState(bool isExpandable, bool isExpanded, int preferredListHeightInRows, string expandButtonHint, string unexpandButtonHint);
	};

	public class ViewItem
	{
		internal WeakReference Data;
		public string Text { get; internal set; }
		public bool IsWarningText { get; internal set; }
		public bool VisiblityControlChecked { get; internal set; }
		public string VisiblityControlHint { get; internal set; }
		public bool PinControlChecked { get; internal set; }
		public string PinControlHint { get; internal set; }
		public bool ProgressVisible { get; internal set; }
		public double ProgressValue { get; internal set; }
	};

	public interface IPresenter
	{
		Task<IMessage> Search(LogViewer.SearchOptions opts);
		void ReceiveInputFocus();
		IMessage FocusedMessage { get; }
		IBookmark FocusedMessageBookmark { get; }
		IBookmark MasterFocusedMessage { get; set; }
		void FindCurrentTime();
		Presenters.LogViewer.IPresenter LogViewerPresenter { get; }

		event EventHandler OnClose;
		event EventHandler OnResizingStarted;
	};

	[Flags]
	public enum MenuItemId
	{
		None,
		Visible = 1,
		Pinned = 2,
		Delete = 4,
		VisibleOnTimeline = 8
	};

	public struct ContextMenuViewData
	{
		public MenuItemId VisibleItems;
		public MenuItemId CheckedItems;
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		ColorThemeMode ColorTheme { get; }
		IReadOnlyList<ViewItem> Items { get; }
		bool IsCombinedProgressIndicatorVisible { get; }

		void OnResizingStarted();
		void OnResizingFinished();
		void OnResizing(int delta);
		void OnToggleBookmarkButtonClicked();
		void OnFindCurrentTimeButtonClicked();
		void OnCloseSearchResultsButtonClicked();
		void OnRefreshButtonClicked();
		void OnExpandSearchesListClicked();
		void OnVisibilityCheckboxClicked(ViewItem item);
		void OnPinCheckboxClicked(ViewItem item);
		void OnDropdownContainerLostFocus();
		void OnDropdownEscape();
		void OnDropdownTextClicked();
		ContextMenuViewData OnContextMenuPopup(ViewItem viewItem);
		void OnMenuItemClicked(ViewItem viewItem, MenuItemId menuItemId);
	};
};