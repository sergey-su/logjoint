using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LogJoint.UI
{
	public partial class RegexBasedFormatPage : UserControl, IProvideSampleLog
	{
		XmlNode formatRoot;
		XmlNode reGrammarRoot;
		bool testOk;
		static readonly string[] parameterStatusStrings = new string[] { "Not set", "OK" };
		static readonly string[] testStatusStrings = new string[] { "", "Passed" };
		static readonly string sampleLogNodeName = "sample-log";
		string sampleLogCache = null;
		Presenters.Help.IPresenter help;

		public RegexBasedFormatPage(Presenters.Help.IPresenter help)
		{
			InitializeComponent();
			this.help = help;
		}

		public void SetFormatRoot(XmlNode formatRoot)
		{
			this.sampleLogCache = null;
			this.formatRoot = formatRoot;
			this.reGrammarRoot = formatRoot.SelectSingleNode("regular-grammar");
			if (this.reGrammarRoot == null)
				this.reGrammarRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("regular-grammar"));
			UpdateView();
		}

		void InitStatusLabel(Label label, bool statusOk, string[] strings)
		{
			label.Text = statusOk ? strings[1] : strings[0];
			label.ForeColor = statusOk ? Color.Green : Color.Black;
		}

		public void UpdateView()
		{
			InitStatusLabel(headerReStatusLabel, reGrammarRoot.SelectSingleNode("head-re[text()!='']") != null, parameterStatusStrings);
			InitStatusLabel(bodyReStatusLabel, reGrammarRoot.SelectSingleNode("body-re[text()!='']") != null, parameterStatusStrings);
			InitStatusLabel(fieldsMappingLabel, reGrammarRoot.SelectSingleNode("fields-config[field[@name='Time']]") != null, parameterStatusStrings);
			InitStatusLabel(testStatusLabel, testOk, testStatusStrings);
			InitStatusLabel(sampleLogStatusLabel, SampleLog != "", parameterStatusStrings);
		}

		private void selectSampleButton_Click(object sender, EventArgs e)
		{
			using (EditSampleLogForm f = new EditSampleLogForm(this))
				f.ShowDialog();
			UpdateView();
		}

		private void testButton_Click(object sender, EventArgs e)
		{
			if (SampleLog == "")
			{
				MessageBox.Show("Provide a sample log first", "", MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			string tmpLog = TempFilesManager.GetInstance().GenerateNewName();
			try
			{
				XDocument clonedFormatXmlDocument = XDocument.Parse(formatRoot.OuterXml);

				UserDefinedFactoryParams createParams;
				createParams.Entry = null;
				createParams.RootNode = clonedFormatXmlDocument.Element("format");
				createParams.FormatSpecificNode = createParams.RootNode.Element("regular-grammar");
				createParams.FactoryRegistry = null;

				// Temporary sample file is always written in Unicode wo BOM: we don't test encoding detection,
				// we test regexps correctness.
				using (StreamWriter w = new StreamWriter(tmpLog, false, new UnicodeEncoding(false, false)))
					w.Write(SampleLog);
				ChangeEncodingToUnicode(createParams);

				using (RegularGrammar.UserDefinedFormatFactory f = new RegularGrammar.UserDefinedFormatFactory(createParams))
				{
					var cp = ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(tmpLog);
					testOk = TestParserForm.Execute(f, cp);
				}

				UpdateView();
			}
			finally
			{
				File.Delete(tmpLog);
			}
		}

		private static void ChangeEncodingToUnicode(UserDefinedFactoryParams createParams)
		{
			var encodingNode = createParams.FormatSpecificNode.Element("encoding");
			if (encodingNode == null)
				createParams.FormatSpecificNode.Add(encodingNode = new XElement("encoding"));
			encodingNode.Value = "UTF-16";
		}

		private void changeHeaderReButton_Click(object sender, EventArgs e)
		{
			using (EditRegexForm f = new EditRegexForm(reGrammarRoot, true, this, help))
			{
				if (f.ShowDialog() != DialogResult.OK)
					return;
				UpdateView();
			}
		}

		private void changeBodyReButon_Click(object sender, EventArgs e)
		{
			using (EditRegexForm f = new EditRegexForm(reGrammarRoot, false, this, help))
			{
				if (f.ShowDialog() != DialogResult.OK)
					return;
				UpdateView();
			}
		}

		IEnumerable<string> GetRegExCaptures(string reId)
		{
			bool bodyReMode = reId == "body-re";
			string reText;
			XmlNode reNode = reGrammarRoot.SelectSingleNode(reId);
			if (reNode == null)
				if (bodyReMode)
					reText = "";
				else
					yield break;
			else
				reText = reNode.InnerText;
			if (bodyReMode && string.IsNullOrWhiteSpace(reText))
				reText = RegularGrammar.FormatInfo.EmptyBodyReEquivalientTemplate;
			Regex re;
			try
			{
				re = new Regex(reText, RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
			}
			catch
			{
				yield break;
			}

			int i = 0;
			foreach (string n in re.GetGroupNames())
				if (i++ > 0)
					yield return n;
		}

		private void changeFieldsMappingButton_Click(object sender, EventArgs e)
		{
			List<string> allCaptures = new List<string>();
			allCaptures.AddRange(GetRegExCaptures("head-re"));
			allCaptures.AddRange(GetRegExCaptures("body-re"));
			using (FieldsMappingForm f = new FieldsMappingForm(reGrammarRoot, allCaptures.ToArray(), help))
			{
				f.ShowDialog();
				UpdateView();
			}
		}

		private void conceptsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			help.ShowHelp("HowRegexParsingWorks.htm");
		}

		#region IProvideSampleLog Members

		public string SampleLog
		{
			get
			{
				if (sampleLogCache == null)
				{
					var sampleLogNode = reGrammarRoot.SelectSingleNode(sampleLogNodeName);
					if (sampleLogNode != null)
						sampleLogCache = sampleLogNode.InnerText;
					else
						sampleLogCache = "";
				}
				return sampleLogCache;
			}
			set
			{
				sampleLogCache = value ?? "";
				var sampleLogNode = reGrammarRoot.SelectSingleNode(sampleLogNodeName);
				if (sampleLogNode == null)
					sampleLogNode = reGrammarRoot.AppendChild(reGrammarRoot.OwnerDocument.CreateElement(sampleLogNodeName));
				sampleLogNode.RemoveAll();
				sampleLogNode.AppendChild(reGrammarRoot.OwnerDocument.CreateCDataSection(sampleLogCache));
			}
		}

		#endregion
	}

	public interface IProvideSampleLog
	{
		string SampleLog { get; set; }
	};
}
