using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
    public partial class EmptyMessagesCollectionMessage : UserControl
    {
        public EmptyMessagesCollectionMessage()
        {
            InitializeComponent();
        }

        public void SetMessage(string msg)
        {
            messageLabel.Text = msg;
            EmptyMessagesCollectionMessage_Resize(null, null);
        }

        private void EmptyMessagesCollectionMessage_Resize(object sender, EventArgs e)
        {
            messageLabel.Location = new Point(
                Math.Max(0, (ClientSize.Width - messageLabel.Size.Width) / 2),
                Math.Max(0, (ClientSize.Height - messageLabel.Size.Height) / 2)
            );
        }
    }
}
