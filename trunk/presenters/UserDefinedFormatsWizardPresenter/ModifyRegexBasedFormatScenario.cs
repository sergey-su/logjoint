using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	/*
	public class ModifyRegexBasedFormatScenario : IFormatsWizardScenario
	{
		public ModifyRegexBasedFormatScenario(IWizardScenarioHost host)
		{
			this.host = host;
			formatDoc = new XmlDocument();
			formatDoc.LoadXml("<format><regular-grammar/></format>");
			regexPage = new RegexBasedFormatPage(host.Help, host.TempFilesManager, host.LogViewerPresenterFactory);
			identityPage = new FormatIdentityPage(host.LogProviderFactoryRegistry, false);
			optionsPage = new FormatAdditionalOptionsPage(host.Help);
			savePage = new SaveFormatPage(host.UserDefinedFormatsManager.Repository, false);
			ResetFormatDocument();
		}

		void ResetFormatDocument()
		{
			regexPage.SetFormatRoot(formatDoc.DocumentElement);
			identityPage.SetFormatRoot(formatDoc.DocumentElement);
			optionsPage.SetFormatRoot(formatDoc.SelectSingleNode("format/regular-grammar"));
			savePage.SetDocument(formatDoc);
		}

		string GetFormatFileNameBasis(IUserDefinedFactory factory)
		{
			string fname = System.IO.Path.GetFileName(factory.Location);

			string suffix = ".format.xml";

			if (fname.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase))
				fname = fname.Remove(fname.Length - suffix.Length);

			return fname;
		}

		public void SetFormat(IUserDefinedFactory factory)
		{
			formatDoc.Load(factory.Location);
			ResetFormatDocument();
			savePage.FileNameBasis = GetFormatFileNameBasis(factory);
		}

		public bool Next()
		{
			if (stage == 1)
			{
				if (savePage.FileNameBasis == "")
					savePage.FileNameBasis = identityPage.GetDefaultFileNameBasis();
			}
			if (stage == 3)
			{
				host.Finish();
				return false;
			}
			++stage;
			return stage < 3;
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
						return regexPage;
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
				WizardScenarioFlag f = WizardScenarioFlag.NextIsActive | WizardScenarioFlag.BackIsActive;
				if (stage == 3)
					f |= WizardScenarioFlag.NextIsFinish;
				return f;
			}
		}

		IWizardScenarioHost host;
		int stage;
		XmlDocument formatDoc;
		RegexBasedFormatPage regexPage;
		FormatIdentityPage identityPage;
		FormatAdditionalOptionsPage optionsPage;
		SaveFormatPage savePage;
	};*/
};