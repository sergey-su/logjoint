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
	public partial class EverythingFilteredOutMessage : UserControl
	{
		public EverythingFilteredOutMessage()
		{
			InitializeComponent();
		}

		private void EverythingFilteredOutMessage_Resize(object sender, EventArgs e)
		{
			tableLayoutPanel1.Location = new Point(
				Math.Max(0, (ClientSize.Width - tableLayoutPanel1.Size.Width) / 2),
				Math.Max(0, (ClientSize.Height - tableLayoutPanel1.Size.Height) / 2)
			);
		}
	}
}
