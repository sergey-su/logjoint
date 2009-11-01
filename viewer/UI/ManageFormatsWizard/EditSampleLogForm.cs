using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class EditSampleLogForm : Form
	{
		IProvideSampleLog provider;

		public EditSampleLogForm(IProvideSampleLog provider)
		{
			this.provider = provider;
			InitializeComponent();
			sampleLogTextBox.Text = provider.SampleLog;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			provider.SampleLog = sampleLogTextBox.Text;
			DialogResult = DialogResult.OK;
		}

		private void loadLigFileButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog1.ShowDialog() != DialogResult.OK)
				return;
			try
			{
				using (StreamReader r = new StreamReader(openFileDialog1.FileName, Encoding.ASCII, true))
				{
					char[] buf = new char[1024 * 4];
					sampleLogTextBox.Text = new string(buf, 0, r.Read(buf, 0, buf.Length));
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Failed to read the file", this.Text, MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
	}
}