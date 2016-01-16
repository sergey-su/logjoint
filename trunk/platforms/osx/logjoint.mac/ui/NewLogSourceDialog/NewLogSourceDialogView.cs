using LogJoint.UI.Presenters.NewLogSourceDialog;

namespace LogJoint.UI
{
	public class NewLogSourceDialogView: IView
	{
		IDialogView IView.CreateDialog(IDialogViewEvents eventsHandler)
		{
			return new NewLogSourceDialogController(eventsHandler);
		}
	};
}