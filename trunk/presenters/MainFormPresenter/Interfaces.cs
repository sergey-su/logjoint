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
	};

	public interface IView
	{
		void SetPresenter(IPresenterEvents presenter);
		IInputFocusState CaptureInputFocusState();
		void ExecuteThreadPropertiesDialog(IThread thread, IPresentersFacade navHandler);
		void SetCancelLongRunningControlsVisibility(bool value);
		void SetAnalizingIndicationVisibility(bool value);
		void BeginSplittingSearchResults();
		void ActivateTab(string tabId);
		void EnableFormControls(bool enable);
		void ShowOptionsMenu();
		void ShowAboutBox();
		void SetCaption(string value);
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
		Escape,
		F,
		K,
		F3,
		F2,
	};


	public interface IPresenterEvents
	{
		void OnClosing();
		void OnLoad();
		void OnTabPressed();
		void OnCancelLongRunningProcessButtonClicked();
		void OnKeyPressed(KeyCode key, bool shift, bool contol);
		void OnOptionsLinkClicked();
		bool OnDragOver(object data);
		void OnDragDrop(object data);
		void OnRawViewButtonClicked();
		void OnAboutMenuClicked();
		void OnConfigurationMenuClicked();
	};

	public interface IDragDropHandler
	{
		bool ShouldAcceptDragDrop(object dataObject);
		void AcceptDragDrop(object dataObject);
	};
};