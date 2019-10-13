using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.MainForm
{
	public interface IPresenter
	{
		void ExecuteThreadPropertiesDialog(IThread thread); // todo: move to a separate presenter
		void ActivateTab(string tabId);
		string AddCustomTab(object uiControl, string caption, object tag);
		void Close();

		event EventHandler Loaded;
		event EventHandler<TabChangingEventArgs> TabChanging;
	};

	public interface IView
	{
		void SetViewModel(IViewModel value);
		IInputFocusState CaptureInputFocusState();
		void ExecuteThreadPropertiesDialog(IThread thread, IPresentersFacade navHandler, IColorTheme theme);
		void SetAnalyzingIndicationVisibility(bool value);
		void BeginSplittingSearchResults();
		void BeginSplittingTabsPanel();
		void ActivateTab(string tabId);
		void AddTab(string tabId, string caption, object uiControl);
		void EnableFormControls(bool enable);
		void EnableOwnedForms(bool enable);
		void ShowOptionsMenu();
		void SetCaption(string value);
		void Close();
		void ForceClose();
		void SetTaskbarState(TaskbarState state);
		void UpdateTaskbarProgress(int progressPercentage);
		void SetShareButtonState(bool visible, bool enabled, bool progress);
		void SetIssueReportingMenuAvailablity(bool value);
	};

	public interface IInputFocusState
	{
		void Restore();
	};

	public static class TabIDs
	{
		public const string Sources = "sources";
		public const string Threads = "threads";
		public const string HighlightingFilteringRules = "highlightingFilteringRules";
		public const string Bookmarks = "bookmarks";
		public const string Search = "search";
	};

	public enum KeyCode
	{
		Unknown,

		Escape,

		ToggleBookmarkShortcut,
		NextBookmarkShortcut,
		PrevBookmarkShortcut,

		HistoryShortcut,

		FindShortcut,
		FindNextShortcut,
		FindPrevShortcut,

		NewWindowShortcut,

		FindCurrentTimeShortcut,
	};


	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		(AutoUpdateButtonState state, string tooltip) AutoUpdateButton { get; }
		void OnClosing();
		void OnLoad();
		void OnTabPressed();
		void OnKeyPressed(KeyCode key);
		void OnOptionsLinkClicked();
		bool OnDragOver(object data);
		void OnDragDrop(object data, bool controlKeyHeld);
		void OnRawViewButtonClicked();
		void OnAboutMenuClicked();
		void OnConfigurationMenuClicked();
		void OnRestartPictureClicked();
		void OnOpenRecentMenuClicked();
		void OnTabChanging(string tabId);
		void OnShareButtonClicked();
		void OnReportProblemMenuItemClicked();
	};

	public interface IDragDropHandler
	{
		bool ShouldAcceptDragDrop(object dataObject);
		void AcceptDragDrop(object dataObject, bool controlKeyHeld);
	};

	public enum TaskbarState
	{
		Progress,
		Idle
	};

	public enum AutoUpdateButtonState
	{
		Hidden,
		ProgressIcon,
		WaitingRestartIcon,
	};

	public class TabChangingEventArgs: EventArgs
	{
		public string TabID { get; private set; }
		public object CustomTabTag { get; private set; }

		public TabChangingEventArgs(string tabId, object customTabTag)
		{
			this.TabID = tabId;
			this.CustomTabTag = customTabTag;
		}
	};
};