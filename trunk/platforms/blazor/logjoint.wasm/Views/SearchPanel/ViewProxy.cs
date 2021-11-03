using LogJoint.UI.Presenters.SearchPanel;
using QuickSearchTextBoxP = LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.Wasm.UI
{
	public class SearchPanelViewProxy : IView
	{
		IViewModel viewModel;

		public SearchPanelViewProxy()
		{
		}

		public void SetComponent(IView component)
		{
			component?.SetViewModel(viewModel);
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}
	}
}
