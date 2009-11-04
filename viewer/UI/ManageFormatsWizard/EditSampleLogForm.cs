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
				using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader r = new StreamReader(fs, Encoding.ASCII, true))
				{
					char[] buf = new char[1024 * 4];
					sampleLogTextBox.Text = new string(buf, 0, r.Read(buf, 0, buf.Length));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to read the file: " + ex.Message, this.Text, MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
	}
}