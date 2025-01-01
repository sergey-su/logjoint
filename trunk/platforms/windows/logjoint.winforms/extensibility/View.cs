using LogJoint.UI;
using LogJoint.UI.Windows;
using LogJoint.UI.Windows.Reactive;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
    class View : UI.Windows.IView
    {
        public View(
            IWinFormsComponentsInitializer winFormsComponentsInitializer,
            IReactive reactiveImpl
        )
        {
            this.winFormsComponentsInitializer = winFormsComponentsInitializer;
            this.reactiveImpl = reactiveImpl;
        }

        void UI.Windows.IView.RegisterToolForm(Form f)
        {
            winFormsComponentsInitializer.InitOwnedForm(f, false);
        }

        IReactive IView.Reactive => reactiveImpl;

        readonly IWinFormsComponentsInitializer winFormsComponentsInitializer;
        readonly IReactive reactiveImpl;
    };
}
