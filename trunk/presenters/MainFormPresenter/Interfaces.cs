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

		event EventHandler Closing;
		event EventHandler<TabChangingEventArgs> TabChanging;
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		IInputFocusState CaptureInputFocusState();
		void ExecuteThreadPropertiesDialog(IThread thread, IPresentersFacade navHandler);
		void SetCancelLongRunningControlsVisibility(bool value);
		void SetAnalizingIndicationVisibility(bool value);
		void BeginSplittingSearchResults();
		void ActivateTab(string tabId);
		void AddTab(string tabId, string caption, object uiControl, object tag);
		void EnableFormControls(bool enable);
		void EnableOwnedForms(bool enable);
		void ShowOptionsMenu();
		void ShowAboutBox();
		void SetCaption(string value);
		void SetUpdateIconVisibility(bool value);
		bool ShowRestartConfirmationDialog(string caption, string text);
		void Close();
		void SetTaskbarState(TaskbarState state);
		void UpdateTaskbarProgress(int progressPercentage);
	};

	public interface IInputFocusState
	{
		void Restore();
	};

	public static class TabIDs
	{
		public const string Sources = "sources";
		public const string Threads = "threads";
		public const string DisplayFilteringRules = "displayFilteringRules";
		public const string HighlightingFilteringRules = "highlightingFilteringRules";
		public const string Bookmarks = "bookmarks";
		public const string Search = "search";
	};

	public enum KeyCode
	{
		Unknown,

		// todo: get rid of specific keys.
		// use only shortcut names.
		Escape,
		F,
		K,
		B,
		F3,
		F2,
		H,

		FindShortcut,
		FindNextShortcut,
		FindPrevShortcut
	};


	public interface IViewEvents
	{
		void OnClosing();
		void OnLoad();
		void OnTabPressed();
		void OnCancelLongRunningProcessButtonClicked();
		void OnKeyPressed(KeyCode key, bool shift = false, bool contol = false);
		void OnOptionsLinkClicked();
		bool OnDragOver(object data);
		void OnDragDrop(object data, bool controlKeyHeld);
		void OnRawViewButtonClicked();
		void OnAboutMenuClicked();
		void OnConfigurationMenuClicked();
		void OnRestartPictureClicked();
		void OnOpenRecentMenuClicked();
		void OnTabChanging(string tabId, object tag);
	};

	public interface IDragDropHandler
	{
		bool ShouldAcceptDragDrop(object dataObject);
		void AcceptDragDrop(object dataObject, bool controlKeyHeld);
	};

	public interface ICommandLineHandler
	{
		void HandleCommandLineArgs(string[] args);
	};

	public enum TaskbarState
	{
		Progress,
		Idle
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