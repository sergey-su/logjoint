using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchResult
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents presenter);
		Presenters.LogViewer.IView MessagesView { get; }
		bool IsMessagesViewFocused { get; }
		void FocusMessagesView();
		void UpdateItems(IList<ViewItem> items);
		void UpdateItem(ViewItem item);
		void UpdateExpandedState(bool isExpandable, bool isExpanded);
	};

	public class ViewItem
	{
		public object Data;
		public string Text;
		public bool IsWarningText;
		public bool VisiblityControlChecked;
		public bool PinControlChecked;
		public bool ProgressVisible;
		public int ProgressValue;
	};

	public interface IPresenter
	{
		Task<IMessage> Search(LogViewer.SearchOptions opts);
		bool IsViewFocused { get; }
		void ReceiveInputFocus();
		IMessage FocusedMessage { get; }
		IBookmark GetFocusedMessageBookmark();

		IBookmark MasterFocusedMessage { get; set; }

		event EventHandler OnClose;
		event EventHandler OnResizingStarted;
	};

	public interface IViewEvents
	{
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
	};
};