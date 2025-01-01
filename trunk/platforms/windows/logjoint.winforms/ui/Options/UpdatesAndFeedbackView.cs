using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.UpdatesAndFeedback;

namespace LogJoint.UI
{
    public partial class UpdatesAndFeedbackView : UserControl, IView
    {
        IViewEvents presenter;

        public UpdatesAndFeedbackView()
        {
            InitializeComponent();
        }

        void IView.SetPresenter(IViewEvents presenter)
        {
            this.presenter = presenter;
        }

        void IView.SetLastUpdateCheckInfo(string breif, string details)
        {
            var text = new StringBuilder(breif);
            var linkArea = new LinkArea();
            if (!string.IsNullOrEmpty(details))
            {
                text.Append(" ");
                int linkBegin = text.Length;
                text.Append("Details.");
                linkArea = new LinkArea(linkBegin, text.Length - linkBegin);
            }
            updateStatusValueLabel.Text = text.ToString();
            updateStatusValueLabel.LinkArea = linkArea;
            updateStatusValueLabel.Tag = details;
        }

        void IView.SetCheckNowButtonAvailability(bool canCheckNow)
        {
            checkForUpdateLinkLabel.Enabled = canCheckNow;
        }

        private void updateStatusValueLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var detailsStr = updateStatusValueLabel.Tag as string;
            if (detailsStr != null)
                MessageBox.Show(detailsStr, "Automatic Software Update", MessageBoxButtons.OK);
        }

        private void checkForUpdateLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            presenter.OnCheckUpdateNowClicked();
        }
    }
}
