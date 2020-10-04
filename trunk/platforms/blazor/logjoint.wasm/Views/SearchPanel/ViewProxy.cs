using LogJoint.UI.Presenters.SearchPanel;
using QuickSearchTextBoxP = LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.Wasm.UI
{
	public class SearchPanelViewProxy : IView
	{
		IViewModel viewModel;
		IView component;
		QuickSearchTextBoxP.IView searchTextBox;

		public SearchPanelViewProxy(QuickSearchTextBoxP.IView searchTextBox)
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

		QuickSearchTextBoxP.IView IView.SearchTextBox => searchTextBox;
	}
}
