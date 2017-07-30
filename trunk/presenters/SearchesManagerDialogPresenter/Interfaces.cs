using System;

namespace LogJoint.UI.Presenters.SearchesManagerDialog
{
	public interface IPresenter
	{
		void Open();
	};

	public interface IView
	{
		IDialogView CreateDialog(IDialogViewEvents eventsHandler);
	};

	public interface IDialogView: IDisposable
	{
		void SetItems(ViewItem[] items);
		ViewItem[] SelectedItems { get; set; }
		void EnableControl(ViewControl id, bool value);
		void OpenModal();
		void CloseModal();
	};

	public class ViewItem
	{
		public string Caption { get; internal set; }
		internal object Data;
	};

	public enum ViewControl
	{
		AddButton,
		DeleteButton,
		EditButton,
		Export,
		Import,
	};

	public interface IDialogViewEvents
	{
		void OnCloseClicked();
		void OnAddClicked();
		void OnDeleteClicked();
		void OnEditClicked();
		void OnSelectionChanged();
		void OnExportClicked();
		void OnImportClicked();
	};
};