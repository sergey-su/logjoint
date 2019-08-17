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
	public partial class SourcesManagementView : UserControl, IView
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

		void IView.SetViewModel(IViewModel value)
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

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			mruContextMenuStrip.Items.Clear();
			foreach (var item in items)
			{
				mruContextMenuStrip.Items.Add(new ToolStripMenuItem(item.Text)
				{
					Tag = item.Data,
					Enabled = !item.Disabled,
					ShortcutKeyDisplayString = item.InplaceAnnotation ?? "",
					ToolTipText = item.ToolTip ?? ""
				});
			}
			mruContextMenuStrip.Show(recentButton, new Point(0, recentButton.Height));
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

		private void mruContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			// Hide the menu here to simplify debugging. Without Hide() the menu remains 
			// on the screen if execution stops at a breakpoint.
			mruContextMenuStrip.Hide();

			viewModel.OnMRUMenuItemClicked(e.ClickedItem.Tag);
		}
	}
}
