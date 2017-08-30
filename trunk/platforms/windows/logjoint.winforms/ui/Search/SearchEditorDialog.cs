using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SearchEditorDialog;

namespace LogJoint.UI
{
	public partial class SearchEditorDialog : Form, IDialogView
	{
		readonly IDialogViewEvents eventsHandler;

		public SearchEditorDialog(IDialogViewEvents eventsHandler)
		{
			InitializeComponent();
			this.eventsHandler = eventsHandler;
		}

		Presenters.FiltersManager.IView IDialogView.FiltersManagerView => filtersManager;

		void IDialogView.CloseModal()
		{
			DialogResult = DialogResult.OK;
		}

		DialogData IDialogView.GetData()
		{
			return new DialogData()
			{
				Name = nameTextBox.Text
			};
		}

		void IDialogView.OpenModal()
		{
			ShowDialog();
		}

		void IDialogView.SetData(DialogData data)
		{
			nameTextBox.Text = data.Name;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnConfirmed();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCancelled();
		}
	}

	public class SearchEditorDialogView : IView
	{
		IDialogView IView.CreateDialog(IDialogViewEvents eventsHandler)
		{
			return new SearchEditorDialog(eventsHandler);
		}
	};
}
