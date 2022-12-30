using LogJoint.UI.Presenters.SearchesManagerDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class SearchesManagerDialog : Form
	{
		readonly IViewModel viewModel;
		readonly Dictionary<ViewControl, Control> controls;
		readonly Windows.Reactive.IListBoxController<IViewItem> listController;

		public SearchesManagerDialog(IViewModel viewModel, Windows.Reactive.IReactive reactive)
		{
			InitializeComponent();
			this.viewModel = viewModel;
			this.controls = new Dictionary<ViewControl, Control>()
			{
				{ ViewControl.AddButton, addButton },
				{ ViewControl.DeleteButton, deleteButton },
				{ ViewControl.EditButton, editButton },
				{ ViewControl.Export, exportButton },
				{ ViewControl.Import, importButton },
			};

			listController = reactive.CreateListBoxController<IViewItem>(listView);

			listController.OnSelect = s => viewModel.OnSelect(s.OfType<IViewItem>());

			var updateItems = Updaters.Create(() => viewModel.Items, listController.Update);
			var updateVisible = Updaters.Create(() => viewModel.IsVisible, value =>
			{
				if (value)
					ShowDialog();
				else
					Hide();
			});
			var updateCloseButton = Updaters.Create(() => viewModel.CloseButtonText, value => closeButton.Text = value);
			var updateControls = Updaters.Create(() => viewModel.EnabledControls, enabled =>
			{
				foreach (var ctrl in controls)
					ctrl.Value.Enabled = enabled.Contains(ctrl.Key);
			});

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateItems();
				updateVisible();
				updateCloseButton();
				updateControls();
			});
		}

		private void addButton_Click(object sender, EventArgs e)
		{
			viewModel.OnAddClicked();
		}

		private void deleteButton_Click(object sender, EventArgs e)
		{
			viewModel.OnDeleteClicked();
		}

		private void editButton_Click(object sender, EventArgs e)
		{
			viewModel.OnEditClicked();
		}

		private void exportButton_Click(object sender, EventArgs e)
		{
			viewModel.OnExportClicked();
		}

		private void importButton_Click(object sender, EventArgs e)
		{
			viewModel.OnImportClicked();
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			viewModel.OnCloseClicked();
		}

		private void SearchesManagerDialog_Closing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			viewModel.OnCancelled();
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (Form.ModifierKeys == Keys.None && keyData == Keys.Escape)
			{
				this.Close();
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}
	}
}
