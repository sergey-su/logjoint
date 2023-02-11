using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchEditorDialog;
using LogJoint.UI.Windows.Reactive;

namespace LogJoint.UI
{
	public partial class SearchEditorDialog : Form
	{
		readonly IViewModel viewModel;

		public SearchEditorDialog(IViewModel viewModel, IReactive reactive)
		{
			InitializeComponent();
			this.viewModel = viewModel;
			filtersManager.SetViewModel(viewModel.FiltersManager, reactive);

			var dialogConroller = new ModalDialogController(this);
			var updateVisible = Updaters.Create(() => this.viewModel.IsVisible, dialogConroller.SetVisibility);
			var updateName = Updaters.Create(() => this.viewModel.Name, (string value) =>
			{
				if (nameTextBox.Text != value)
					nameTextBox.Text = value;
			});

			this.viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateName();
				updateVisible();
			});
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			viewModel.OnConfirmed();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			viewModel.OnCancelled();
		}

		private void nameTextBox_TextChanged(object sender, System.EventArgs e)
		{
			viewModel.OnChangeName(nameTextBox.Text);
		}
	}
}
