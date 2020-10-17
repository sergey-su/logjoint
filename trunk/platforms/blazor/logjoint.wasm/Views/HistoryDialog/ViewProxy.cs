using LogJoint.UI.Presenters.HistoryDialog;
using QuickSearchTextBoxP = LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.Wasm.UI
{
	public class HistoryDialogViewProxy : IView
	{
		IViewModel viewModel;
		IView component;
		readonly QuickSearchTextBoxP.IView searchTextBox;

		public HistoryDialogViewProxy(QuickSearchTextBoxP.IView searchTextBox)
		{
			this.searchTextBox = searchTextBox;
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

		QuickSearchTextBoxP.IView IView.QuickSearchTextBox => searchTextBox;

		void IView.PutInputFocusToItemsList() => component?.PutInputFocusToItemsList();
	}
}
