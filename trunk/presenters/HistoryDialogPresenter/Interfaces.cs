using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.HistoryDialog
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents presenter);
		QuickSearchTextBox.IView QuickSearchTextBox { get; }
		void AboutToShow();
		void Update(ViewItem[] items);
		void Show();
		void Hide();
		ViewItem[] SelectedItems { get; set;  }
		void PutInputFocusToItemsList();
		void EnableOpenButton(bool enable);
		bool ShowClearHistoryConfirmationDialog(string message);
		void ShowOpeningFailurePopup(string message);
	};

	public interface IPresenter
	{
		void ShowDialog();
	};

	public interface IViewEvents
	{
		void OnOpenClicked();
		void OnDoubleClick();
		void OnDialogShown();
		void OnFindShortcutPressed();
		void OnSelectedItemsChanged();
		void OnClearHistoryButtonClicked();
	};

	public class ViewItem
	{
		public ViewItemType Type;
		public string Text;
		public string Annotation;
		public object Data;
	};

	public enum ViewItemType
	{
		Log,
		Workspace,
		HistoryComment
	};
};