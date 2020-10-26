using LogJoint.UI.Presenters.QuickSearchTextBox;
using System;
using System.Threading.Tasks;

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

		async void IView.ReceiveInputFocus()
		{
			if (component == null)
			{
				// Wait one JS frame for the textbox may be rendered.
				// Example when it helps: Ctrl+F activates the search page and puts the focus to the text box.
				await Task.Yield();
			}
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
