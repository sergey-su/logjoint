using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcesManager;

namespace LogJoint.UI
{
	public partial class SourcesManagementView : UserControl
	{
		IViewModel viewModel;

		public SourcesManagementView()
		{ 
			InitializeComponent();
		}

		public SourcesListView SourcesListView
		{
			get { return sourcesListView; }
		}

		public void SetViewModel(IViewModel value)
		{
			this.viewModel = value;

			var updateDeleteAllButton = Updaters.Create(
				() => value.DeleteAllSourcesButtonEnabled,
				enabled => deleteAllButton.Enabled = enabled
			);
			var enableDeleteSelectedButton = Updaters.Create(
				() => value.DeleteSelectedSourcesButtonEnabled,
				enabled => deleteButton.Enabled = enabled
			);
			var updateShareButton = Updaters.Create(
				() => value.ShareButtonState,
				state =>
				{
					shareButton.Visible = state.visible;
					shareButton.Enabled = state.enabled;
					if (state.progress)
					{
						shareButton.Image = Properties.Resources.loader;
						shareButton.ImageAlign = ContentAlignment.MiddleLeft;
					}
					else
					{
						shareButton.Image = null;
					}
				}
			);
			var updatePropertiesButton = Updaters.Create(
				() => value.PropertiesButtonEnabled,
				enabled => propertiesButton.Enabled = enabled
			);

			value.ChangeNotification.CreateSubscription(() =>
			{
				updateDeleteAllButton();
				enableDeleteSelectedButton();
				updateShareButton();
				updatePropertiesButton();
			});
		}

		private void addNewLogButton_Click(object sender, EventArgs e)
		{
			viewModel.OnAddNewLogButtonClicked();
		}

		private void shareButton_Click(object sender, EventArgs e)
		{
			viewModel.OnShareButtonClicked();
		}

		private void propertiesButton_Click(object sender, EventArgs e)
		{
			viewModel.OnPropertiesButtonClicked();
		}

		private void deleteButton_Click(object sender, EventArgs e)
		{
			viewModel.OnDeleteSelectedLogSourcesButtonClicked();
		}

		private void deleteAllButton_Click(object sender, EventArgs e)
		{
			viewModel.OnDeleteAllLogSourcesButtonClicked();
		}

		private void recentButton_Click(object sender, EventArgs e)
		{
			viewModel.OnShowHistoryDialogButtonClicked();
		}
	}
}
