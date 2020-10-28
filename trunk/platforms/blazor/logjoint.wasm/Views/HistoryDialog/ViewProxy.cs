using LogJoint.UI.Presenters.HistoryDialog;
using QuickSearchTextBoxP = LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.Wasm.UI
{
	public class HistoryDialogViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public HistoryDialogViewProxy()
		{
		}

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}

		QuickSearchTextBoxP.IView IView.QuickSearchTextBox => null;

		void IView.PutInputFocusToItemsList() => component?.PutInputFocusToItemsList();
	}
}
