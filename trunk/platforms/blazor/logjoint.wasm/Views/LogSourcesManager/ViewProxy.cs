using System.Collections.Generic;
using LogJoint.UI.Presenters.SourcesManager;

namespace LogJoint.Wasm.UI
{
	public class SourcesManagerViewProxy : IView
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

		void IView.ShowMRUMenu(List<MRUMenuItem> items)
		{
			component.ShowMRUMenu(items);
		}
	}
}
