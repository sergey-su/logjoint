using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog
{
    public partial class EVTFactoryUI : UserControl, IView
    {
        IViewEvents eventsHandler;

        public EVTFactoryUI()
        {
            InitializeComponent();

            var extFilter = new StringBuilder();
            string[] exts = new string[] { ".evtx", ".evt" };
            foreach (string ext in exts)
                extFilter.AppendFormat("*{0} Files|*{0}|", ext);
            extFilter.Append("All Files (*.*)|*.*");
            openFileDialog.Filter = extFilter.ToString();
        }

        object IView.PageView
        {
            get { return this; }
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }

        void IView.SetSelectedLogText(string value)
        {
            logTextBox.Text = value;
        }

        private void openButton1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                eventsHandler.OnIdentitySelected(WindowsEventLog.EventLogIdentity.FromFileName(openFileDialog.FileName));
            }
        }

        private void openButton2_Click(object sender, EventArgs e)
        {
            using (SelectLogSourceDialog dlg = new SelectLogSourceDialog())
            {
                var logIdentity = dlg.ShowDialog();
                if (logIdentity != null)
                {
                    eventsHandler.OnIdentitySelected(logIdentity);
                }
            }
        }
    }
}
