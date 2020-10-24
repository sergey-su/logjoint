using LogJoint.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.Wasm.UI
{
	public class MessagePropertiesViewProxy : IView, IDialog
	{
		IDialogViewModel viewModel;

		public IDialogViewModel ViewModel => viewModel;

		IDialog IView.CreateDialog(IDialogViewModel value)
		{
			viewModel = value;
			return this;
		}

		bool IDialog.IsDisposed => false;

		void IDialog.Show()
		{
		}
	}
}
