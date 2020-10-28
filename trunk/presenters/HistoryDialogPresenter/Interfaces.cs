using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.HistoryDialog
{
	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		QuickSearchTextBox.IView QuickSearchTextBox { get; }
		void PutInputFocusToItemsList();
	};

	public interface IPresenter
	{
		void ShowDialog();
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		QuickSearchTextBox.IViewModel QuickSearchTextBox { get; }

		bool IsVisible { get; }
		bool OpenButtonEnabled { get; }
		IViewItem RootViewItem { get; }

		// support for non-reactive UIs
		IReadOnlyList<IViewItem> ItemsIgnoringTreeState { get; }

		void OnSelect(IEnumerable<IViewItem> items);
		void OnExpand(IViewItem item);
		void OnCollapse(IViewItem item);
		void OnOpenClicked();
		void OnCancelClicked();
		void OnDoubleClick();
		void OnDialogShown();
		void OnFindShortcutPressed();
		void OnClearHistoryButtonClicked();
	};

	public interface IViewItem: ITreeNode
	{
		ViewItemType Type { get; }
		string Text { get; }
		string Annotation { get; }
	};

	public enum ViewItemType
	{
		Leaf,
		Comment,
		ItemsContainer
	};
};