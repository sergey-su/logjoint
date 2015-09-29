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

		[Export("deleteSelectedSourcesButton")]
		NSButton deleteSelectedSourcesButton { get; set; }

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
			// todo
			return true;
		}

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			// todo
		}

		void IView.ShowMRUOpeningFailurePopup()
		{
			// todo
		}

		void IView.EnableDeleteAllSourcesButton(bool enable)
		{
			// todo
		}

		void IView.EnableDeleteSelectedSourcesButton(bool enable)
		{
			deleteSelectedSourcesButton.Enabled = enable;
		}

		void IView.EnableTrackChangesCheckBox(bool enable)
		{
			// todo
		}

		void IView.SetTrackingChangesCheckBoxState(TrackingChangesCheckBoxState state)
		{
			// todo
		}

		void IView.SetShareButtonState(bool visible, bool enabled)
		{
			// todo
		}

		string IView.ShowOpenSingleFileDialog()
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;

			if (dlg.RunModal () == 1) 
			{
				var url = dlg.Urls [0];
				if (url != null)
				{
					return url.Path;
				}
			}

			return null;
		}

		#endregion

		[Action ("addLogSourceButtonClicked:")]
		void addLogSourceButtonClicked (NSObject sender)
		{
			viewEvents.OnOpenSingleFileButtonClicked();
		}

		[Action ("deleteSelectedSourcesButtonClicked:")]
		void deleteSelectedSourcesButtonClicked (NSObject sender)
		{
			viewEvents.OnDeleteSelectedLogSourcesButtonClicked();
		}
	}
}

