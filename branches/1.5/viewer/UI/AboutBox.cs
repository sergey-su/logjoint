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
				"Assembly version: {1}{0}"+
				"http://logjoint.codeplex.com/",
				Environment.NewLine,
				Assembly.GetExecutingAssembly().GetName().Version
			);

		}
	}
}