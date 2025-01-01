using LogJoint.UI.Presenters;
using LogJoint.Drawing;
using System.Windows.Forms;

namespace LogJoint.UI
{
    public partial class ThreadPropertiesForm : Form
    {
        IThread thread;
        IPresentersFacade handler;
        IColorTheme theme;

        public ThreadPropertiesForm(IThread thread, IPresentersFacade handler, IColorTheme theme)
        {
            this.thread = thread;
            this.handler = handler;
            this.theme = theme;
            InitializeComponent();
            UpdateView();
        }

        static void SetBookmark(LinkLabel label, IBookmark bmk)
        {
            label.Tag = bmk;
            if (bmk != null)
            {
                label.Text = bmk.Time.ToUserFrendlyString();
                label.Enabled = true;
            }
            else
            {
                label.Text = "-";
                label.Enabled = false;
            }
        }

        void UpdateView()
        {
            idTextBox.Text = thread.ID;
            idTextBox.Select(0, 0);
            nameTextBox.Text = thread.Description;
            colorPanel.BackColor = theme.ThreadColors.GetByIndex(thread.ThreadColorIndex).ToSystemDrawingObject();
            SetBookmark(firstMessageLinkLabel, thread.FirstKnownMessage);
            SetBookmark(lastMessageLinkLabel, thread.LastKnownMessage);
            logSourceLink.Text = thread.LogSource.DisplayName;
        }

        private void linkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            IBookmark bmk = ((LinkLabel)sender).Tag as IBookmark;
            if (bmk != null)
                handler.ShowMessage(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.NoLinksInPopups | BookmarkNavigationOptions.GenericStringsSet);
        }

        private void logSourceLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            handler.ShowLogSource(thread.LogSource);
        }

    }

}