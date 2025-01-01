using LogJoint.UI.Presenters.PreprocessingUserInteractions;

namespace LogJoint.Wasm.UI
{
    public class PreprocessingUserInteractionsViewProxy : IView
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
