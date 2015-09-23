using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.SourcesManager;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public class SourcesManagementControlAdapter: NSObject, IView
	{
		[Export("sourcesListPlaceholder")]
		NSView sourcesListPlaceholder { get; set;}

		[Export("view")]
		public SourcesManagementControl View { get; set;}

		SourcesListControlAdapter sourcesListControlAdapter;
		IViewEvents viewEvents;

		public SourcesManagementControlAdapter()
		{
			NSBundle.LoadNib ("SourcesManagementControl", this);
			sourcesListControlAdapter = new SourcesListControlAdapter();
			sourcesListControlAdapter.View.MoveToPlaceholder(sourcesListPlaceholder);
		}

		public SourcesListControlAdapter SourcesListControlAdapter
		{
			get { return sourcesListControlAdapter; }
		}


		#region IView implementation

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		bool IView.ShowDeletionConfirmationDialog(int nrOfSourcesToDelete)
		{
			return true;
		}

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
		}

		void IView.ShowMRUOpeningFailurePopup()
		{
		}

		void IView.EnableDeleteAllSourcesButton(bool enable)
		{
		}

		void IView.EnableDeleteSelectedSourcesButton(bool enable)
		{
		}

		void IView.EnableTrackChangesCheckBox(bool enable)
		{
		}

		void IView.SetTrackingChangesCheckBoxState(TrackingChangesCheckBoxState state)
		{
		}

		void IView.SetShareButtonState(bool visible, bool enabled)
		{
		}

		#endregion

		[Action ("addLogSourceButtonClicked:")]
		void addLogSourceButtonClicked (NSObject sender)
		{
			viewEvents.OnAddNewLogButtonClicked();
		}

		[Action ("deleteSelectedSourcesButtonClicked:")]
		void deleteSelectedSourcesButtonClicked (NSObject sender)
		{
			viewEvents.OnDeleteSelectedLogSourcesButtonClicked();
		}
	}
}

