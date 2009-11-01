using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class SourceDetailsForm : Form
	{
		ILogSource source;

		public SourceDetailsForm(ILogSource src)
		{
			this.source = src;
			InitializeComponent();
		}

		private void stateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
		}

	}
}