using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.SourcesManager
{
	public interface IPresenter
	{
		void StartDeletionInteraction(ILogSource[] forSources);

		event EventHandler<BusyStateEventArgs> OnBusyState;
	};

	public interface IView
	{
		void SetViewModel(IViewModel value);
		void ShowMRUMenu(List<MRUMenuItem> items);
	};

	public struct MRUMenuItem
	{
		public object Data;
		public string Text;
		public string InplaceAnnotation;
		public string ToolTip;
		public bool Disabled;
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }

		bool DeleteSelectedSourcesButtonEnabled { get; }
		bool PropertiesButtonEnabled { get; }
		bool DeleteAllSourcesButtonEnabled { get; }
		(bool visible, bool enabled, bool progress) ShareButtonState { get; }

		void OnAddNewLogButtonClicked();
		void OnDeleteSelectedLogSourcesButtonClicked();
		void OnDeleteAllLogSourcesButtonClicked();
		void OnMRUButtonClicked();
		void OnMRUMenuItemClicked(object data);
		void OnShareButtonClicked();
		void OnShowHistoryDialogButtonClicked();
		void OnPropertiesButtonClicked();
	};
};