using LogJoint.UI.Presenters.BookmarksList;

namespace LogJoint.Wasm.UI
{
	public class BookmarksListViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}
	}
}
