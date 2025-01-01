using LogJoint.UI.Presenters.SourcePropertiesWindow;

namespace LogJoint.Wasm.UI
{
    public class SourcePropertiesWindowViewProxy : IView
    {
        IViewModel viewModel;
        IView component;

        public SourcePropertiesWindowViewProxy()
        {
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

        IWindow IView.CreateWindow() => component?.CreateWindow();
    }
}
