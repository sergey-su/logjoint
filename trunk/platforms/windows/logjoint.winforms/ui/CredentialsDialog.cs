using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class CredentialsDialog : Form
	{
		public CredentialsDialog()
		{
			InitializeComponent();
		}

		public bool Execute(string site)
		{
			siteTextBox.Text = site;
			return ShowDialog() == System.Windows.Forms.DialogResult.OK;
		}

		public string UserName { get { return userNameTextBox.Text; } }
		public string Password { get { return passwordTextBox.Text; } }
	}
}
