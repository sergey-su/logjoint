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
		void SetSearchResultText(string value);
		void SetSearchStatusText(string value);
		void SetSearchCompletionPercentage(int value);
		void SetSearchProgressBarVisiblity(bool value);
		void SetSearchStatusLabelVisibility(bool value);
		bool IsMessagesViewFocused { get; }
		void FocusMessagesView();
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
	};
};