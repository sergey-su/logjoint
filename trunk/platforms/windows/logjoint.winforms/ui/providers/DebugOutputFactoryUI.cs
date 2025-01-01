using System.Windows.Forms;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.DebugOutput
{
    public partial class DebugOutputFactoryUI : UserControl, IView
    {
        public DebugOutputFactoryUI()
        {
            InitializeComponent();
        }

        object IView.PageView
        {
            get { return this; }
        }
    }
}
