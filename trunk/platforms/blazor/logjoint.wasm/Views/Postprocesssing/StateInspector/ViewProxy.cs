using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;

namespace LogJoint.Wasm.UI.Postprocesssing.StateInspector
{
	public class ViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		void IView.ScrollStateHistoryItemIntoView(int itemIndex)
		{
			component?.ScrollStateHistoryItemIntoView(itemIndex);
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
			component?.SetViewModel(value);
		}

		void IView.Show()
		{
			component?.Show();
		}
	}
}
