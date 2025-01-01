using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.FormatDeleteConfirmPage;

namespace LogJoint.UI
{
    public partial class FormatDeleteConfirmPage : UserControl, IView
    {
        public FormatDeleteConfirmPage()
        {
            InitializeComponent();
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
        }

        void IView.Update(string messageLabelText, string descriptionTextBoxValue, string fileNameTextBoxValue, string dateTextBoxValue)
        {
            messageLabel.Text = messageLabelText;
            descriptionTextBox.Text = descriptionTextBoxValue;
            fileNameTextBox.Text = fileNameTextBoxValue;
            dateTextBox.Text = dateTextBoxValue;
        }
    }
}
