using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.MSLogParser
{
	public partial class LogParsedBaseFactoryUI : UserControl, ILogReaderFactoryUI
	{
		private MSLogParser.UserDefinedFormatFactory factory;

		public LogParsedBaseFactoryUI(MSLogParser.UserDefinedFormatFactory factory)
		{
			InitializeComponent();
			this.factory = factory;

			List<string> exts = new List<string>(factory.SupportedPatterns);
			if (exts.Count > 0)
			{
				StringBuilder extFilter = new StringBuilder();
				foreach (string ext in exts)
					extFilter.AppendFormat("*{0} Files|*{0}|", ext);
				extFilter.Append("All Files (*.*)|*.*");
				openFileDialog.Filter = extFilter.ToString();
			}

			if (factory.KnownInputName == "EVT")
			{
				openButton2.Text = "Select Live Log...";
				openButton2.Click += delegate(object sender, EventArgs e)
				{
					using (MSLogParser.EVT.SelectLogSourceDialog dlg = new MSLogParser.EVT.SelectLogSourceDialog())
					{
						string[] logs = dlg.ShowDialog();
						if (logs != null)
							AddSources(logs);
					}
				};
				openButton2.Visible = true;
			}
		}

		#region ILogReaderFactoryUI Members

		public Control UIControl
		{
			get { return this; }
		}

		public void Apply(IFactoryUICallback callback)
		{
			ILogReaderHost host = null;
			ILogReader reader = null;
			try
			{
				if (mergeCheckBox.Checked)
				{
					ConnectionParams p = new ConnectionParams();
					InitConnectionParams(p);
					int idx = 0;
					foreach (string source in sourcesTextBox.Lines)
					{
						if (string.IsNullOrEmpty(source))
							continue;
						p["from" + (idx++).ToString()] = source;
					}
					if (callback.FindExistingReader(p) == null)
					{
						host = callback.CreateHost();
						reader = factory.CreateFromConnectionParams(host, p);
						callback.AddNewReader(reader);
					}
				}
				else
				{
					foreach (string source in sourcesTextBox.Lines)
					{
						if (string.IsNullOrEmpty(source))
							continue;
						ConnectionParams p = new ConnectionParams();
						InitConnectionParams(p);
						p["from0"] = source;
						if (callback.FindExistingReader(p) != null)
							continue;
						host = callback.CreateHost();
						reader = factory.CreateFromConnectionParams(host, p);
						callback.AddNewReader(reader);
						reader = null;
						host = null;
					}
				}
			}
			catch
			{
				if (reader != null)
					reader.Dispose();
				if (host != null)
					host.Dispose();
				throw;
			}
			sourcesTextBox.Text = "";
		}

		#endregion

		protected void AddSources(IEnumerable<string> sources)
		{
			StringBuilder tmp = new StringBuilder(sourcesTextBox.Text);
			foreach (string n in sources)
			{
				if (tmp.Length > 0)
					tmp.AppendLine();
				tmp.Append(n);
			}
			sourcesTextBox.Text = tmp.ToString();
		}

		protected virtual void InitConnectionParams(ConnectionParams p)
		{
		}

		private void openButton1_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				AddSources(openFileDialog.FileNames);
			}
		}
	}
}
