using System;
using Foundation;
using AppKit;
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

		[Export("recentSourcesButton")]
		NSButton recentSourcesButton { get; set; }

		[Export("logSourcePropertiesButton")]
		NSButton logSourcePropertiesButton { get; set; }

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

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			recentSourcesButton.Image.Template = true;
		}


		#region IView implementation

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			// not supported in this UI
		}

		void IView.EnableDeleteAllSourcesButton(bool enable)
		{
			// not supported in this UI
		}

		void IView.EnableDeleteSelectedSourcesButton(bool enable)
		{
			deleteSelectedSourcesButton.Enabled = enable;
		}

		void IView.EnableTrackChangesCheckBox(bool enable)
		{
			// not supported in this UI
		}

		void IView.SetTrackingChangesCheckBoxState(TrackingChangesCheckBoxState state)
		{
			// not supported in this UI
		}

		void IView.SetShareButtonState(bool visible, bool enabled, bool progress)
		{
			// not supported in this UI
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

		void IView.SetPropertiesButtonState(bool enabled)
		{
			logSourcePropertiesButton.Enabled = enabled;
		}

		#endregion

		[Action ("OnAddLogSourceButtonClicked:")]
		void OnAddLogSourceButtonClicked (NSObject sender)
		{
			viewEvents.OnAddNewLogButtonClicked();
		}

		[Action ("OnDeleteSelectedSourcesButtonClicked:")]
		void OnDeleteSelectedSourcesButtonClicked (NSObject sender)
		{
			viewEvents.OnDeleteSelectedLogSourcesButtonClicked();
		}

		[Action ("OnRecentSourcesButtonClicked:")]
		void OnRecentSourcesButtonClicked (NSObject sender)
		{
			viewEvents.OnShowHistoryDialogButtonClicked();
		}

		[Action ("OnLogSourcePropertiesButtonClicked:")]
		void OnLogSourcePropertiesButtonClicked (NSObject sender)
		{
			viewEvents.OnPropertiesButtonClicked();
		}
	}
}

