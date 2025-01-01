using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.FormatIdentityPage;

namespace LogJoint.UI
{
    public partial class FormatIdentityPage : UserControl, IView
    {
        IViewEvents eventsHandler;

        public FormatIdentityPage()
        {
            InitializeComponent();
        }

        Control GetCtrl(ControlId ctrl)
        {
            switch (ctrl)
            {
                case ControlId.HeaderLabel: return headerLabel;
                case ControlId.CompanyNameEdit: return companyNameTextBox;
                case ControlId.FormatNameEdit: return formatNameTextBox;
                case ControlId.DescriptionEdit: return descriptionTextBox;
                default: return null;
            }
        }

        string IView.this[ControlId id]
        {
            get
            {
                return GetCtrl(id)?.Text ?? "";
            }
            set
            {
                var ctrl = GetCtrl(id);
                if (ctrl != null)
                    ctrl.Text = value;
            }
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        void IView.SetFocus(ControlId id)
        {
            GetCtrl(id)?.Focus();
        }
    }

}
