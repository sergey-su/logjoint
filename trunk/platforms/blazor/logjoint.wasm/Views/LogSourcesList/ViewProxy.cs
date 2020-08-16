using LogJoint.UI.Presenters.SourcesList;
using System;

namespace LogJoint.Wasm.UI
{
	public class SourcesListViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		void IView.SetTopItem(IViewItem item)
		{
			component.SetTopItem(item);
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}
	}
}
