using LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.Wasm.UI
{
	public class QuickSearchTextBoxViewProxy : IView
	{
		IViewModel viewModel;
		IView component;

		public void SetComponent(IView component)
		{
			this.component = component;
			component?.SetViewModel(viewModel);
		}

		public IViewModel ViewModel => viewModel;

		void IView.ReceiveInputFocus()
		{
			component?.ReceiveInputFocus();
		}

		void IView.SelectAll()
		{
			component?.SelectAll();
		}

		void IView.SelectEnd()
		{
			component?.SelectEnd();
		}

		void IView.SetViewModel(IViewModel value)
		{
			viewModel = value;
		}
	}
}
