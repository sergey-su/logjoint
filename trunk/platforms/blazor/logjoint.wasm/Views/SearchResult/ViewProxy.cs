using LogJoint.UI.Presenters.SearchResult;
using ViewerP = LogJoint.UI.Presenters.LogViewer;

namespace LogJoint.Wasm.UI
{
	public class SearchResultViewProxy : IView
	{
		IViewModel viewModel;
		IView component;
		ViewerP.IView viewer;

		public SearchResultViewProxy(ViewerP.IView viewer)
		{
			this.viewer = viewer;
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

		ViewerP.IView IView.MessagesView => viewer;

		void IView.UpdateExpandedState(bool isExpandable, bool isExpanded, int preferredListHeightInRows, string expandButtonHint, string unexpandButtonHint)
		{
			component?.UpdateExpandedState(isExpandable, isExpanded, preferredListHeightInRows, expandButtonHint, unexpandButtonHint);
		}
	}
}
