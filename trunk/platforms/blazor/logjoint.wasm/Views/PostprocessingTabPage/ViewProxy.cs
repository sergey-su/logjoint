using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;

namespace LogJoint.Wasm.UI.Postprocessing
{
    public class ViewProxy : IView
    {
        public void SetComponent(IView component)
        {
            this.component = component;
            component?.SetViewModel(viewModel);
        }

        void IView.SetViewModel(IViewModel value)
        {
            viewModel = value;
        }

        object IView.UIControl => null;

        public IViewModel viewModel;
        IView component;
    }
}
