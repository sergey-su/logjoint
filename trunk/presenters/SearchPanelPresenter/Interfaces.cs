using System;

namespace LogJoint.UI.Presenters.SearchPanel
{
	public interface IPresenter
	{
		event EventHandler InputFocusAbandoned;
		void ReceiveInputFocusByShortcut(bool forceSearchAllOccurencesMode = false);
		void PerformSearch();
		void PerformReversedSearch();
		void CollapseSearchResultPanel();
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
	};

	[Flags]
	public enum ViewCheckableControl
	{
		None = 0,
		MatchCase = 1,
		WholeWord = 2,
		RegExp = 4,
		SearchWithinThisThread = 8,
		Errors = 16,
		Warnings = 32,
		Infos = 64,
		Frames = 128,
		QuickSearch = 256,
		SearchUp = 512,
		SearchFromCurrentPosition = 1024,
		SearchInSearchResult = 2048,
		SearchAllOccurences = 4096,
		SearchWithinCurrentLog = 8192,
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		QuickSearchTextBox.IViewModel QuickSearchTextBox { get; }

		ViewCheckableControl CheckableControlsState { get; } // bitmask. Bit is set if control is checked.
		ViewCheckableControl EnableCheckableControls { get; } // bitmask. Bit is set if control is enabled.
		(bool isVisible, string text) FiltersLink { get; }

		void OnSearchButtonClicked();
		void OnCheckControl(ViewCheckableControl ctrl, bool checkedValue);
		void OnFiltersLinkClicked();
	};
};