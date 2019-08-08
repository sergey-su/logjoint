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
		IViewModel viewModel;

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

		void IView.SetViewModel (IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateDeleteSelectedSourcesButton = Updaters.Create (
				() => viewModel.DeleteSelectedSourcesButtonEnabled,
				value => deleteSelectedSourcesButton.Enabled = value
			);

			var updatePropertiesButton = Updaters.Create (
				() => viewModel.PropertiesButtonEnabled,
				value => logSourcePropertiesButton.Enabled = value
			);

			viewModel.ChangeNotification.CreateSubscription (() => {
				updateDeleteSelectedSourcesButton ();
				updatePropertiesButton ();
			});
		}

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			// not supported in this UI
		}

		#endregion

		[Action ("OnAddLogSourceButtonClicked:")]
		void OnAddLogSourceButtonClicked (NSObject sender)
		{
			viewModel.OnAddNewLogButtonClicked();
		}

		[Action ("OnDeleteSelectedSourcesButtonClicked:")]
		void OnDeleteSelectedSourcesButtonClicked (NSObject sender)
		{
			viewModel.OnDeleteSelectedLogSourcesButtonClicked();
		}

		[Action ("OnRecentSourcesButtonClicked:")]
		void OnRecentSourcesButtonClicked (NSObject sender)
		{
			viewModel.OnShowHistoryDialogButtonClicked();
		}

		[Action ("OnLogSourcePropertiesButtonClicked:")]
		void OnLogSourcePropertiesButtonClicked (NSObject sender)
		{
			viewModel.OnPropertiesButtonClicked();
		}
	}
}

