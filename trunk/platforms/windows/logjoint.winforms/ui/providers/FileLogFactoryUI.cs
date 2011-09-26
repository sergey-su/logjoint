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
				StringBuilder buf = new StringBuilder();
				if (fnames.Length == 1)
				{
					buf.Append(fnames[0]);
				}
				else
				{
					foreach (string n in fnames)
					{
						buf.AppendFormat("{0}\"{1}\"", buf.Length==0 ? "" : " ", n);
					}
				}
				filePathTextBox.Text = buf.ToString();
			}
		}

		#region ILogReaderFactoryUI Members

		public object UIControl
		{
			get { return this; }
		}

		static readonly Regex fileListRe = new Regex(@"^\s*\""\s*([^\""]+)\s*\""");

		IEnumerable<string> ParseFileList(string str)
		{
			int idx = 0;
			Match m = fileListRe.Match(str, idx);
			if (m.Success)
			{
				do
				{
					yield return m.Groups[1].Value;
					idx += m.Length + 1;
					if (idx >= str.Length)
						break;
					m = fileListRe.Match(str, idx, str.Length - idx);
				} while (m.Success);
			}
			else
			{
				yield return str;
			}
		}

		public void Apply(IFactoryUICallback hostsFactory)
		{
			string tmp = filePathTextBox.Text.Trim();
			if (tmp == "")
				return;
			filePathTextBox.Text = "";
			foreach (string fname in ParseFileList(tmp))
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

		#endregion
	}
}
