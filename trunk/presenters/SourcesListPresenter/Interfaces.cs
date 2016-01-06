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
		IViewItem CreateItem(string key, ILogSource logSource, ILogSourcePreprocessing logSourcePreprocessing);
		int ItemsCount { get; }
		IViewItem GetItem(int idx);
		void RemoveAt(int idx);
		int IndexOfKey(string key);
		void Add(IViewItem item);
		void SetTopItem(IViewItem item);
		void InvalidateFocusedMessageArea();
		string ShowSaveLogDialog(string suggestedLogFileName);
		void ShowSaveLogError(string msg);
	};

	public interface IViewItem
	{
		ILogSource LogSource { get; }
		ILogSourcePreprocessing LogSourcePreprocessing { get; }
		bool Selected { get; set; }
		bool? Checked { get; set; }
		void SetText(string value);
		void SetBackColor(ModelColor color);
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
		ShowAllLogs = 128
	};

	public interface IViewEvents
	{
		void OnSourceProprtiesMenuItemClicked();
		void OnEnterKeyPressed();
		void OnDeleteButtonPressed();
		void OnMenuItemOpening(bool ctrl, out MenuItem visibleItems, out MenuItem checkedItems);
		void OnItemChecked(IViewItem item);
		void OnSourceVisisbleMenuItemClicked(bool menuItemChecked);
		void OnFocusedMessageSourcePainting(out ILogSource logSourceToPaint);
		void OnSaveLogAsMenuItemClicked();
		void OnSaveMergedFilteredLogMenuItemClicked();
		void OnOpenContainingFolderMenuItemClicked();
		void OnSelectionChanged();
		void OnShowOnlyThisLogClicked();
		void OnShowAllLogsClicked();
	};
};