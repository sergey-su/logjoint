using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
		void SetRawViewButtonState(bool visible, bool checked_);
		void SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked);
		bool IsMessagesViewFocused { get; }
	};

	public interface IPresenter
	{
		LogViewer.SearchResult Search(LogViewer.SearchOptions opts);
		bool IsViewFocused { get; }
		IMessage FocusedMessage { get; }
		bool RawViewAllowed { get; set; }

		IMessage MasterFocusedMessage { get; set; }

		event EventHandler OnClose;
		event EventHandler OnResizingStarted;
	};

	public interface IViewEvents
	{
		void OnToggleRawViewButtonClicked();
		void OnResizingStarted();
		void OnResizingFinished();
		void OnResizing(int delta);
		void OnColoringButtonClicked(LogViewer.ColoringMode mode);
		void OnToggleBookmarkButtonClicked();
		void OnFindCurrentTimeButtonClicked();
		void OnCloseSearchResultsButtonClicked();
		void OnRefreshButtonClicked();
	};
};