using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Xml;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ImportNLogPage : UserControl
	{
		IWizardScenarioHost host;

		public ImportNLogPage(IWizardScenarioHost host)
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
			XmlNamespaceManager nsMgr = new XmlNamespaceManager(new NameTable());
			nsMgr.AddNamespace("nlog", "http://www.nlog-project.org/schemas/NLog.xsd");
			foreach (XmlElement e in doc.SelectNodes("//nlog:target[@layout]", nsMgr))
			{
				AvailablePattern p = new AvailablePattern();
				string value = e.GetAttribute("layout");
				if (string.IsNullOrWhiteSpace(value))
					continue;
				p.Value = value;
				p.AppenderName = e.GetAttribute("name");
				if (string.IsNullOrWhiteSpace(p.AppenderName))
					p.AppenderName = "unnamed";
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
				MessageBox.Show("Provide a layout string, please", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public string Pattern
		{
			get { return patternTextbox.Text; }
		}
	}

	public class ImportNLogScenario : IFormatsWizardScenario
	{
		XmlDocument doc;
		IWizardScenarioHost host;

		public ImportNLogScenario(IWizardScenarioHost host)
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

			importPage = new ImportNLogPage(host);
			identityPage = new FormatIdentityPage(true);
			identityPage.SetFormatRoot(doc.DocumentElement);
			optionsPage = new FormatAdditionalOptionsPage();
			optionsPage.SetFormatRoot(doc.SelectSingleNode("format/regular-grammar"));
			savePage = new SaveFormatPage(false);
			importLogPage = new NLogGenerationLogPage(host);
			savePage.SetDocument(doc);
		}

		bool GenerateGrammar()
		{
			try
			{
				NLog.LayoutImporter.GenerateRegularGrammarElement(doc.DocumentElement, importPage.Pattern, importLog);
			}
			catch (NLog.ImportErrorDetectedException)
			{
				return true;
			}
			catch (NLog.ImportException e)
			{
				MessageBox.Show("Failed to import the layout:\n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public bool Next()
		{
			int nextStage = stage + 1;
			switch (stage)
			{
				case 0:
					if (!importPage.ValidateInput())
						return false;
					if (!GenerateGrammar())
						return false;
					if (NeedToShowImportLogPage)
					{
						importLogPage.UpdateView(importPage.Pattern, importLog);
						nextStage = 1;
					}
					else
					{
						nextStage = 2;
					}
					break;
				case 1:
					if (importLog.HasErrors)
						return false;
					break;
				case 2:
					if (savePage.FileNameBasis == "")
						savePage.FileNameBasis = identityPage.GetDefaultFileNameBasis();
					break;
				case 3:
					break;
				case 4:
					host.Finish();
					break;
			}
			if (stage == 4)
				return false;
			stage = nextStage;
			return true;
		}

		public bool Prev()
		{
			if (stage == 0)
				return false;
			if (stage == 2 && !NeedToShowImportLogPage)
				stage -= 2;
			else
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
						return importLogPage;
					case 2:
						return identityPage;
					case 3:
						return optionsPage;
					case 4:
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
				if (stage == 4)
					f |= WizardScenarioFlag.NextIsFinish;
				if (stage == 1 && importLog.HasErrors)
					f &= ~WizardScenarioFlag.NextIsActive;
				return f;
			}
		}

		bool NeedToShowImportLogPage { get { return importLog.HasErrors || importLog.HasWarnings; } }

		NLog.ImportLog importLog = new NLog.ImportLog();
		int stage;
		FormatIdentityPage identityPage;
		ImportNLogPage importPage;
		NLogGenerationLogPage importLogPage;
		FormatAdditionalOptionsPage optionsPage;
		SaveFormatPage savePage;
	};
}
