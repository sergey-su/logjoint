using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.SourcesList
{
	public interface IPresenter
	{
		IReadOnlyList<ILogSource> SelectedSources { get; }
		IReadOnlyList<ILogSourcePreprocessing> SelectedPreprocessings { get; }
		void SelectSource(ILogSource source);
		void SelectPreprocessing(ILogSourcePreprocessing source);
		void SaveLogSourceAs(ILogSource logSource);

		event EventHandler DeleteRequested;
	};

	public interface IView
	{
		void SetViewModel(IViewModel value);
		void SetTopItem(IViewItem item);
	};

	public interface IViewItem: Reactive.ITreeNode
	{
		bool? Checked { get; }
		(Color value, bool isFailureColor) Color { get; }
		IViewItem Parent { get; }
		string Description { get; }
		string Annotation { get; }
	};

	[Flags]
	public enum MenuItem
	{
		None,
		SourceVisible = 1,
		SaveLogAs = 2,
		SourceProperties = 4,
		Separator1 = 8,
		OpenContainingFolder = 16,
		SaveMergedFilteredLog = 32,
		ShowOnlyThisLog = 64,
		ShowAllLogs = 128,
		CopyErrorMessage = 256,
		CloseOthers = 512,
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }

		IViewItem RootItem { get; }
		IViewItem FocusedMessageItem { get; }

		void OnSourceProprtiesMenuItemClicked();
		void OnEnterKeyPressed();
		void OnDeleteButtonPressed();
		(MenuItem visibleItems, MenuItem checkedItems) OnMenuItemOpening(bool ctrl);
		void OnItemCheck(IViewItem item, bool value);
		void OnItemExpand(IViewItem item);
		void OnItemCollapse(IViewItem item);
		void OnSourceVisisbleMenuItemClicked(bool menuItemChecked);
		void OnSaveLogAsMenuItemClicked();
		void OnSaveMergedFilteredLogMenuItemClicked();
		void OnOpenContainingFolderMenuItemClicked();
		void OnSelectionChange(IReadOnlyList<IViewItem> proposedSelection);
		void OnShowOnlyThisLogClicked();
		void OnShowAllLogsClicked();
		void OnCopyShortcutPressed();
		void OnCopyErrorMessageClicked();
		void OnCloseOthersClicked();
		void OnSelectAllShortcutPressed();
	};
};