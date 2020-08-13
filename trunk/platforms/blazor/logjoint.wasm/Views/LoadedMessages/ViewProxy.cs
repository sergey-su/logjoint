using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogJoint.Settings;
using LogJoint.UI.Presenters.LoadedMessages;

namespace LogJoint.Wasm.UI.LoadedMessages
{
    public class ViewProxy : IView
    {
        public ViewProxy(LogJoint.UI.Presenters.LogViewer.IView messagesView)
        {
            this.messagesView = messagesView;
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

        LogJoint.UI.Presenters.LogViewer.IView IView.MessagesView => messagesView;

        public IViewModel viewModel;
        IView component;
        LogJoint.UI.Presenters.LogViewer.IView messagesView;
    }
}
