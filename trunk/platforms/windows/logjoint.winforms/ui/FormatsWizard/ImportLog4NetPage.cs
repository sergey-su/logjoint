using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace LogJoint.UI
{
	public partial class ImportLog4NetPage : UserControl
	{
		IWizardScenarioHost host;

		public ImportLog4NetPage(IWizardScenarioHost host)
		{
			InitializeComponent();
			this.host = host;
		}

		class AvailablePattern
		{
			public string AppenderName;
			public string Value;
			public override string ToString()
			{
				return string.Format("{0}: {1}", AppenderName, Value);
			}
		};

		private void openConfigButton_Click(object sender, EventArgs evt)
		{
			if (openFileDialog1.ShowDialog() != DialogResult.OK)
				return;

			string fileName = openFileDialog1.FileName;
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(fileName);
			}
			catch (XmlException e)
			{
				MessageBox.Show("Failed to load the config: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			List<AvailablePattern> avaPatterns = new List<AvailablePattern>();
			foreach (XmlElement e in doc.SelectNodes("configuration/log4net/appender[@name]/layout[@type='log4net.Layout.PatternLayout'][@value | conversionPattern[@value]]"))
			{
				AvailablePattern p = new AvailablePattern();
				XmlNode valueNode = e.SelectSingleNode("@value | conversionPattern/@value");
				if (valueNode == null)
					continue;
				p.Value = valueNode.Value;
				if (string.IsNullOrEmpty(p.Value))
					continue;
				p.AppenderName = ((XmlElement)e.ParentNode).GetAttribute("name");
				avaPatterns.Add(p);
			}
			if (avaPatterns.Count == 0)
			{
				MessageBox.Show("No layout patterns found in the config", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			availablePatternsListBox.Items.Clear();
			availablePatternsListBox.Items.AddRange(avaPatterns.ToArray());
			availablePatternsListBox.SelectedIndex = 0;
			configFileTextBox.Text = openFileDialog1.FileName;
		}

		private void availablePatternsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (availablePatternsListBox.SelectedIndex >= 0)
				patternTextbox.Text = ((AvailablePattern)availablePatternsListBox.Items[availablePatternsListBox.SelectedIndex]).Value;
		}

		private void availablePatternsListBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Clicks >= 2 && patternTextbox.Text.Length > 0)
				host.Next();
		}

		public bool ValidateInput()
		{
			if (patternTextbox.Text.Length == 0)
			{
				MessageBox.Show("Provide a pattern string, please", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public string Pattern
		{
			get { return patternTextbox.Text; }
		}

		public bool GenerateGrammar(XmlElement root)
		{
			try
			{
				Log4NetPatternImporter.GenerateRegularGrammarElement(root, Pattern);
			}
			catch (Log4NetImportException e)
			{
				MessageBox.Show("Failed to import the pattern:\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}
	}

	public class ImportLog4NetScenario: IFormatsWizardScenario
	{
		XmlDocument doc;
		IWizardScenarioHost host;

		public ImportLog4NetScenario(IWizardScenarioHost host)
		{
			this.host = host;
			doc = new XmlDocument();
			doc.LoadXml(@"
<format>
	<regular-grammar>
		<head-re></head-re>
		<body-re></body-re>
		<fields-config></fields-config>
	</regular-grammar>
</format>");

			importPage = new ImportLog4NetPage(host);
			identityPage = new FormatIdentityPage(true);
			identityPage.SetFormatRoot(doc.DocumentElement);
			optionsPage = new FormatAdditionalOptionsPage();
			optionsPage.SetFormatRoot(doc.SelectSingleNode("format/regular-grammar"));
			savePage = new SaveFormatPage(false);
			savePage.SetDocument(doc);
		}

		public bool Next()
		{
			switch (stage)
			{
				case 0:
					if (!importPage.ValidateInput() || !importPage.GenerateGrammar(doc.DocumentElement))
						return false;
					break;
				case 1:
					if (savePage.FileNameBasis == "")
						savePage.FileNameBasis = identityPage.GetDefaultFileNameBasis();
					break;
				case 2:
					break;
				case 3:
					host.Finish();
					break;
			}
			if (stage == 3)
				return false;
			++stage;
			return true;
		}

		public bool Prev()
		{
			if (stage == 0)
				return false;
			--stage;
			return true;
		}

		public Control Current
		{
			get 
			{
				switch (stage)
				{
					case 0:
						return importPage;
					case 1:
						return identityPage;
					case 2:
						return optionsPage;
					case 3:
						return savePage;
				}
				return null;
			}
		}
		public WizardScenarioFlag Flags
		{
			get
			{
				WizardScenarioFlag f = WizardScenarioFlag.BackIsActive | WizardScenarioFlag.NextIsActive;
				if (stage == 3)
					f |= WizardScenarioFlag.NextIsFinish;
				return f;
			}
		}

		int stage;
		FormatIdentityPage identityPage;
		ImportLog4NetPage importPage;
		FormatAdditionalOptionsPage optionsPage;
		SaveFormatPage savePage;
	};
}
