using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public partial class FileLogFactoryUI : UserControl, ILogProviderFactoryUI
	{
		IFileBasedLogProviderFactory factory;

		public FileLogFactoryUI(IFileBasedLogProviderFactory factory)
		{
			this.factory = factory;
			InitializeComponent();
			UpdateView();
		}
		
		
		private void browseButton_Click(object sender, EventArgs e)
		{
			char[] wildcarsChars = new char[] {'*', '?'};

			StringBuilder concretePatterns = new StringBuilder();
			StringBuilder wildcarsPatterns = new StringBuilder();

			foreach (string s in factory.SupportedPatterns)
			{
				StringBuilder buf = null;
				if (s.IndexOfAny(wildcarsChars) >= 0)
				{
					if (s != "*.*" && s != "*")
						buf = wildcarsPatterns;
				}
				else
				{
					buf = concretePatterns;
				}
				if (buf != null)
				{
					buf.AppendFormat("{0}{1}", buf.Length == 0 ? "" : "; ", s);
				}
			}

			StringBuilder filter = new StringBuilder();
			if (concretePatterns.Length > 0)
				filter.AppendFormat("{0}|{0}|", concretePatterns.ToString());

			if (wildcarsPatterns.Length > 0)
				filter.AppendFormat("{0}|{0}|", wildcarsPatterns.ToString());

			filter.Append("*.*|*.*");

			browseFileDialog.Filter = filter.ToString();

			if (browseFileDialog.ShowDialog() == DialogResult.OK)
			{
				string[] fnames = browseFileDialog.FileNames;
				filePathTextBox.Text = FileListUtils.MakeFileList(fnames).ToString();
			}
		}

		#region ILogReaderFactoryUI Members

		public object UIControl
		{
			get { return this; }
		}

		public void Apply(IFactoryUICallback hostsFactory)
		{
			if (independentLogModeRadioButton.Checked)
				ApplyIndependentLogsMode(hostsFactory);
			else if (rotatedLogModeRadioButton.Checked)
				ApplyRotatedLogMode(hostsFactory);
		}

		void ApplyIndependentLogsMode(IFactoryUICallback hostsFactory)
		{
			string tmp = filePathTextBox.Text.Trim();
			if (tmp == "")
				return;
			filePathTextBox.Text = "";
			foreach (string fname in FileListUtils.ParseFileList(tmp))
			{
				IConnectionParams connectParams = factory.CreateParams(fname);

				if (hostsFactory.FindExistingProvider(connectParams) != null)
					continue;

				ILogProviderHost host = null;
				ILogProvider provider = null;
				try
				{
					host = hostsFactory.CreateHost();
					provider = factory.CreateFromConnectionParams(host, connectParams);
					hostsFactory.AddNewProvider(provider);
				}
				catch
				{
					if (provider != null)
						provider.Dispose();
					if (host != null)
						host.Dispose();
					throw;
				}
			}
		}

		void ApplyRotatedLogMode(IFactoryUICallback hostsFactory)
		{
			var folder = folderPartTextBox.Text.Trim();
			if (folder == "")
				return;
			if (!System.IO.Directory.Exists(folder))
			{
				MessageBox.Show("Specified folder does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			folderPartTextBox.Text = "";

			folder = folder.TrimEnd('\\');

			IConnectionParams connectParams = factory.CreateRotatedLogParams(folder);
			if (hostsFactory.FindExistingProvider(connectParams) != null)
				return;

			ILogProviderHost host = null;
			ILogProvider provider = null;
			try
			{
				host = hostsFactory.CreateHost();
				provider = factory.CreateFromConnectionParams(host, connectParams);
				hostsFactory.AddNewProvider(provider);
			}
			catch (Exception e)
			{
				if (provider != null)
					provider.Dispose();
				if (host != null)
					host.Dispose();
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		void UpdateView()
		{
			bool supportsRotation = (factory.Flags & LogProviderFactoryFlag.SupportsRotation) != 0;
			rotatedLogModeRadioButton.Enabled = supportsRotation;
			if (!supportsRotation)
				independentLogModeRadioButton.Checked = true;

			filePathTextBox.Enabled = independentLogModeRadioButton.Checked;
			browseFileButton.Enabled = independentLogModeRadioButton.Checked;

			folderPartTextBox.Enabled = rotatedLogModeRadioButton.Checked;
			browseFolderButton.Enabled = rotatedLogModeRadioButton.Checked;
		}

		#endregion

		private void RadioButtonCheckedChanged(object sender, EventArgs e)
		{
			UpdateView();
		}

		private void browseFolderButton_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				folderPartTextBox.Text = folderBrowserDialog.SelectedPath;
		}
	}
}
