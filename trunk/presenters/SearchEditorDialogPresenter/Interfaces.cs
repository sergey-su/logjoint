using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SearchEditorDialog
{
	public interface IView
	{
		IDialogView CreateDialog(IDialogViewEvents eventsHandler);
	};

	public interface IDialogView: IDisposable
	{
		FiltersManager.IView FiltersManagerView { get; }
		void SetData(DialogData name);
		DialogData GetData();
		void OpenModal();
		void CloseModal();
	};

	public struct DialogData
	{
		public string Name;
	};

	public interface IPresenter
	{
		bool Open(IUserDefinedSearch search);
	};

	public interface IDialogViewEvents
	{
		void OnConfirmed();
		void OnCancelled();
	};
};