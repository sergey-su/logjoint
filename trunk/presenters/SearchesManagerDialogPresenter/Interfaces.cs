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
		ViewItem[] GetSelectedItems();
		void EnableControl(ViewControl id, bool value);
		void OpenModal();
		void CloseModal();
	};

	public class ViewItem
	{
		public string Caption { get; internal set; }
		internal object Data;
	};

	[Flags]
	public enum ViewControl
	{
		AddButton = 1,
		DeleteButton = 2,
		EditButton = 4,
	};

	public interface IDialogViewEvents
	{
		void OnCloseClicked();
		void OnAddClicked();
		void OnDeleteClicked();
		void OnEditClicked();
		void OnSelectionChanged();
	};
};