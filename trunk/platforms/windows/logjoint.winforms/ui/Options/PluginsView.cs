using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.Plugins;

namespace LogJoint.UI
{
	public partial class PluginsView : UserControl, IView
	{
		IViewModel viewModel;
		Windows.Reactive.IListBoxController pluginsListController;

		public PluginsView()
		{
			InitializeComponent();
		}

		public void Init(Windows.Reactive.IReactive reactive)
		{
			pluginsListController = reactive.CreateListBoxController(pluginsListBox);
			pluginsListController.OnSelect = items => viewModel.OnSelect(items.FirstOrDefault() as IPluginListItem);
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateFetchStatus = Updaters.Create(
				() => viewModel.Status,
				status =>
				{
					statusLabel.Visible = status.text != null;
					statusLabel.Text = status.text ?? "";
					statusLabel.ForeColor = (status.flags & StatusFlags.IsError) != 0 ? Color.Red : SystemColors.ControlText;
					fetchStatusProgresssLabel.Visible = (status.flags & StatusFlags.IsProgressIndicatorVisible) != 0;
				}
			);

			var updateList = Updaters.Create(
				() => viewModel.ListItems,
				pluginsListController.Update
			);

			var updateSelectedPluginControls = Updaters.Create(
				() => viewModel.SelectedPluginData,
				data =>
				{
					captionLabel.Text = data.Caption;
					descriptionTextBox.Text = data.Description;
					actionButton.Enabled = data.ActionButton.Enabled;
					actionButton.Text = data.ActionButton.Caption;
				}
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateFetchStatus();
				updateList();
				updateSelectedPluginControls();
			});
		}

		private void actionButton_Click(object sender, EventArgs e) => viewModel.OnAction();
	}
}
