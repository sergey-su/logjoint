using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.EditSampleDialog;

namespace LogJoint.UI
{
    public partial class EditSampleLogForm : Form, IView
    {
        IViewEvents eventsHandler;

        public EditSampleLogForm()
        {
            InitializeComponent();
        }

        string IView.SampleLogTextBoxValue
        {
            get { return sampleLogTextBox.Text; }
            set { sampleLogTextBox.Text = value; }
        }

        void IView.Show()
        {
            ShowDialog();
        }

        void IView.Close()
        {
            base.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnCloseButtonClicked(accepted: true);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnCloseButtonClicked(accepted: false);
        }

        private void loadLigFileButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnLoadSampleButtonClicked();
        }

        private void sampleLogTextBox_TextChanged(object sender, EventArgs e)
        {
            var originalSample = sampleLogTextBox.Text;
            var fixedSample = StringUtils.NormalizeLinebreakes(originalSample);
            if (fixedSample != originalSample)
                sampleLogTextBox.Text = fixedSample;
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }
    }
}