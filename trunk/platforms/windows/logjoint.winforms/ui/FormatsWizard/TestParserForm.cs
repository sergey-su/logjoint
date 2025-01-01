using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.TestDialog;

namespace LogJoint.UI
{
    public partial class TestParserForm : Form, IView
    {
        IViewEvents eventsHandler;

        public TestParserForm()
        {
            InitializeComponent();
        }

        Presenters.LogViewer.IView IView.LogViewer => viewerControl;

        void IView.Close()
        {
            base.Close();
        }

        void IView.SetData(string message, TestOutcome testOutcome)
        {
            statusTextBox.Text = message;
            if (testOutcome != TestOutcome.None)
                if (testOutcome == TestOutcome.Success)
                    statusPictureBox.Image = LogJoint.Properties.Resources.OkCheck32x32;
                else
                    statusPictureBox.Image = LogJoint.Properties.Resources.Error;
            else
                statusPictureBox.Image = null;
        }

        void IView.Show()
        {
            ShowDialog();
        }

        private void CloseButton_Click(object sender, System.EventArgs e)
        {
            eventsHandler.OnCloseButtonClicked();
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }
    }
}