using System;
using System.Drawing;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.NLogGenerationLogPage;

namespace LogJoint.UI
{
    public partial class NLogGenerationLogPage : UserControl, IView
    {
        IViewEvents eventsHandler;

        public NLogGenerationLogPage()
        {
            InitializeComponent();
        }

        void IView.SelectLayoutTextRange(int idx, int len)
        {
            layoutTextbox.Select(idx, len);
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        static int GetImageIdx(IconType t)
        {
            switch (t)
            {
                case IconType.ErrorIcon: return 0;
                case IconType.WarningIcon: return 1;
                case IconType.NeutralIcon: return 2;
                default: return -1;
            }
        }

        void IView.Update(string layoutTextboxValue, string headerLabelValue, IconType headerIcon, MessagesListItem[] messagesList)
        {
            headerPanel.Visible = headerLabelValue != null;
            headerLabel.Text = headerLabelValue ?? "";
            headerLabel.ImageIndex = GetImageIdx(headerIcon);

            layoutTextbox.Text = layoutTextboxValue;
            layoutTextbox.Select(0, 0);

            flowLayoutPanel.SuspendLayout();

            flowLayoutPanel.Controls.Clear();

            foreach (var message in messagesList)
            {
                var linkLabel = new LinkLabel();

                linkLabel.Links.Clear();

                linkLabel.Text = message.Text;
                foreach (var link in message.Links)
                    linkLabel.Links.Add(new LinkLabel.Link(link.Item1, link.Item2, link.Item3));

                linkLabel.ImageList = imageList1;
                linkLabel.ImageIndex = GetImageIdx(message.Icon);
                linkLabel.ImageAlign = ContentAlignment.TopLeft;
                linkLabel.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
                linkLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
                linkLabel.AutoSize = true;
                linkLabel.LinkClicked += (s, e) => (e.Link.LinkData as Action)?.Invoke();

                flowLayoutPanel.Controls.Add(linkLabel);
            }

            flowLayoutPanel.ResumeLayout();
        }
    }
}
