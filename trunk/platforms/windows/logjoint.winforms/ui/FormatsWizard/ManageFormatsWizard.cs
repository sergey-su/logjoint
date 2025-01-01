using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard;

namespace LogJoint.UI
{
    public partial class ManageFormatsWizard : Form, IView
    {
        IViewEvents eventsHandler;

        public ManageFormatsWizard(
        )
        {
            InitializeComponent();
        }

        private void containerPanel_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder3D(e.Graphics, containerPanel.ClientRectangle,
                Border3DStyle.Etched, Border3DSide.Bottom);
        }

        private void containerPanel_Resize(object sender, EventArgs e)
        {
            containerPanel.Invalidate();
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnBackClicked();
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnNextClicked();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            eventsHandler.OnCloseClicked();
        }


        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        void IView.ShowDialog()
        {
            this.ShowDialog();
        }

        void IView.CloseDialog()
        {
            this.Close();
        }

        void IView.SetControls(string backText, bool backEnabled, string nextText, bool nextEnabled)
        {
            backButton.Enabled = backEnabled;
            backButton.Text = backText;
            nextButton.Enabled = nextEnabled;
            nextButton.Text = nextText;
        }

        void IView.HidePage(object viewObject)
        {
            var ctrl = viewObject as Control;
            if (ctrl != null)
                ctrl.Visible = false;
        }

        void IView.ShowPage(object viewObject)
        {
            var ctrl = viewObject as Control;
            if (ctrl == null)
                return;
            ctrl.Parent = containerPanel;
            ctrl.Dock = DockStyle.Fill;
            ctrl.Visible = true;
            ctrl.Focus();
        }
    }
}