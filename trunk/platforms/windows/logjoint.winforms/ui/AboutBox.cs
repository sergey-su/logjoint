using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace LogJoint.UI
{
	public partial class AboutBox : Form
	{
		public AboutBox()
		{
			InitializeComponent();

			textBox.Text = string.Format(
				"LogJoint{0}" +
				"Log viewer tool for professionals.{0}" +
				"Assembly version: {1}{0}" +
				"http://logjoint.codeplex.com/"+
				"{2}",
				Environment.NewLine,
				Assembly.GetExecutingAssembly().GetName().Version,
				GetSharingText()
			);
		}

		string GetSharingText()
		{
			var installerUrl = LogJoint.Properties.Settings.Default.InstallerUrl;
			if (string.IsNullOrEmpty(installerUrl))
				return "";
			return string.Format("{0}{0}Share the tool with other professionals: {1}",
				Environment.NewLine,
				installerUrl);
		}
	}
}