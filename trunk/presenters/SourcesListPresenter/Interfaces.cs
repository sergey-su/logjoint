using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.SourcesList
{
	public interface IPresenter
	{
		void UpdateView();
		IEnumerable<ILogSource> SelectedSources { get; }
		IEnumerable<ILogSourcePreprocessing> SelectedPreprocessings { get; }
		void SelectSource(ILogSource source);
		void SelectPreprocessing(ILogSourcePreprocessing source);
		void SaveLogSourceAs(ILogSource logSource);

		event EventHandler DeleteRequested;
		event EventHandler SelectionChanged;
		event EventHandler<BusyStateEventArgs> OnBusyState;
	};

	public interface IView
	{
		void SetPresenter(IViewEvents presenter);
		void BeginUpdate();
		void EndUpdate();
		IEnumerable<IViewItem> Items { get; }
		IViewItem AddItem(object datum, IViewItem parent);
		void Remove(IViewItem item);
		void SetTopItem(IViewItem item);
		void InvalidateFocusedMessageArea();
	};

	public interface IViewItem
	{
		object Datum { get; }
		bool Selected { get; set; }
		bool? Checked { get; set; }
		void SetText(string value);
		void SetBackColor(ModelColor color, bool isFailureColor);
	};

	[Flags]
	public enum MenuItem
	{
		None,
		SourceVisible = 1,
		SaveLogAs = 2,
		SourceProprties = 4,
		Separator1 = 8,
		OpenContainingFolder = 16,
		SaveMergedFilteredLog = 32,
		ShowOnlyThisLog = 64,
		ShowAllLogs = 128,
		CopyErrorMessage = 256,
		CloseOthers = 512,
	};

	public interface IViewEvents
	{
		void OnSourceProprtiesMenuItemClicked();
		void OnEnterKeyPressed();
		void OnDeleteButtonPressed();
		void OnMenuItemOpening(bool ctrl, out MenuItem visibleItems, out MenuItem checkedItems);
		void OnItemChecked(IViewItem item);
		void OnSourceVisisbleMenuItemClicked(bool menuItemChecked);
		IViewItem OnFocusedMessageSourcePainting();
		void OnSaveLogAsMenuItemClicked();
		void OnSaveMergedFilteredLogMenuItemClicked();
		void OnOpenContainingFolderMenuItemClicked();
		void OnSelectionChanged();
		void OnShowOnlyThisLogClicked();
		void OnShowAllLogsClicked();
		void OnCopyShortcutPressed();
		void OnCopyErrorMessageCliecked();
		void OnCloseOthersClicked();
		void OnSelectAllShortcutPressed();
	};
};