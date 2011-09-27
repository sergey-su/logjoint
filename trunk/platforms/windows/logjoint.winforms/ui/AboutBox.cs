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
			
			UpdateMemoryConsumptionLink();
		}

		void UpdateMemoryConsumptionLink()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("Managed memory consumption: ");
			StringUtils.FormatBytesUserFriendly(GC.GetTotalMemory(false), buf);
			buf.Append(" ");
			int linkStart = buf.Length;
			buf.Append("collect unused");
			int linkLen = buf.Length - linkStart;
			memoryConsumptionLinkLabel.Text = buf.ToString();
			memoryConsumptionLinkLabel.LinkArea = new LinkArea(linkStart, linkLen);
		}

		private void memoryConsumptionLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			GC.Collect(2, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced);
			UpdateMemoryConsumptionLink();
		}
	}
}