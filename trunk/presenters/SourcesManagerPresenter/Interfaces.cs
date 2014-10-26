using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.SourcesManager
{
	public class BusyStateEventArgs : EventArgs
	{
		readonly bool busyStateRequired;

		public bool BusyStateRequired
		{
			get { return busyStateRequired; }
		}

		public BusyStateEventArgs(bool busyStateRequired)
		{
			this.busyStateRequired = busyStateRequired;
		}
	};

	public interface IPresenter
	{
		void UpdateView();
		event EventHandler<BusyStateEventArgs> OnBusyState;
	};

	public interface IView
	{
		bool ShowDeletionConfirmationDialog(int nrOfSourcesToDelete);
		void ShowMRUMenu(List<MRUMenuItem> items);
		void ShowMRUOpeningFailurePopup();
		void EnableDeleteAllSourcesButton(bool enable);
		void EnableDeleteSelectedSourcesButton(bool enable);
		void EnableTrackChangesCheckBox(bool enable);
		void SetTrackingChangesCheckBoxState(TrackingChangesCheckBoxState state);
	};

	public struct MRUMenuItem
	{
		public string ID;
		public string Text;
		public bool Disabled;
	};

	public enum TrackingChangesCheckBoxState
	{
		Indeterminate,
		Checked,
		Unchecked
	};

	public interface IPresenterEvents
	{
		void OnAddNewLogButtonClicked();
		void OnDeleteSelectedLogSourcesButtonClicked();
		void OnDeleteAllLogSourcesButtonClicked();
		void OnMRUButtonClicked();
		void OnMRUMenuItemClicked(string itemId);
		void OnTrackingChangesCheckBoxChecked(bool value);
	};
};