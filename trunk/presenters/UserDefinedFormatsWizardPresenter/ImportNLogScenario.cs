using System;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class ImportNLogScenario : IFormatsWizardScenario
	{
		XmlDocument doc;
		IWizardScenarioHost host;
		IAlertPopup alerts;

		public ImportNLogScenario(IWizardScenarioHost host, IObjectFactory fac, IAlertPopup alerts)
		{
			this.host = host;
			this.alerts = alerts;
			doc = new XmlDocument();
			doc.LoadXml(@"
<format>
	<regular-grammar>
		<head-re></head-re>
		<body-re></body-re>
		<fields-config></fields-config>
		<dejitter jitter-buffer-size='20'/>
	</regular-grammar>
</format>");

			importPage = fac.CreateImportNLogPage(host);
			identityPage = fac.CreateFormatIdentityPage(host, true);
			identityPage.SetFormatRoot(doc.DocumentElement);
			optionsPage = fac.CreateFormatAdditionalOptionsPage(host);
			optionsPage.SetFormatRoot(doc.SelectSingleNode("format/regular-grammar"));
			savePage = fac.CreateSaveFormatPage(host, false);
			importLogPage = fac.CreateNLogGenerationLogPage(host);
			savePage.SetDocument(doc);
		}

		bool GenerateGrammar()
		{
			try
			{
				NLog.LayoutImporter.GenerateRegularGrammarElementForSimpleLayout(doc.DocumentElement, importPage.Pattern, importLog);
			}
			catch (NLog.ImportErrorDetectedException)
			{
				return true;
			}
			catch (NLog.ImportException e)
			{
				alerts.ShowPopup("Error", "Failed to import the layout:\n" + e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
				return false;
			}
			return true;
		}

		bool IFormatsWizardScenario.Next()
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

		bool IFormatsWizardScenario.Prev()
		{
			if (stage == 0)
				return false;
			if (stage == 2 && !NeedToShowImportLogPage)
				stage -= 2;
			else
				--stage;
			return true;
		}

		void IFormatsWizardScenario.SetCurrentFormat(IUserDefinedFactory udf)
		{
		}

		IWizardPagePresenter IFormatsWizardScenario.Current
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
		WizardScenarioFlag IFormatsWizardScenario.Flags
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
		FormatIdentityPage.IPresenter identityPage;
		ImportNLogPage.IPresenter importPage;
		NLogGenerationLogPage.IPresenter importLogPage;
		FormatAdditionalOptionsPage.IPresenter optionsPage;
		SaveFormatPage.IPresenter savePage;
	};
};