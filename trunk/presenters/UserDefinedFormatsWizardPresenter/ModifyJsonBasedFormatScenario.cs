using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class ModifyJsonBasedFormatScenario : IFormatsWizardScenario
	{
		public ModifyJsonBasedFormatScenario(
			IWizardScenarioHost host,
			IObjectFactory fac
		)
		{
			this.host = host;
			formatDoc = new XmlDocument();
			formatDoc.LoadXml("<format><json/></format>");
			jsonPage = fac.CreateJsonBasedFormatPage(host);
			identityPage = fac.CreateFormatIdentityPage(host, newFormatMode: false);
			optionsPage = fac.CreateFormatAdditionalOptionsPage(host);
			savePage = fac.CreateSaveFormatPage(host, newFormatMode: false);
			ResetFormatDocument();
		}

		void ResetFormatDocument()
		{
			jsonPage.SetFormatRoot(formatDoc.DocumentElement);
			identityPage.SetFormatRoot(formatDoc.DocumentElement);
			optionsPage.SetFormatRoot(formatDoc.SelectSingleNode("format/json"));
			savePage.SetDocument(formatDoc);
		}

		bool IFormatsWizardScenario.Next()
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

		bool IFormatsWizardScenario.Prev()
		{
			if (stage == 0)
				return false;
			--stage;
			return true;
		}

		void IFormatsWizardScenario.SetCurrentFormat(IUserDefinedFactory factory)
		{
			formatDoc.Load(factory.Location);
			ResetFormatDocument();
			savePage.FileNameBasis = CustomFormatPageUtils.GetFormatFileNameBasis(factory);
		}

		IWizardPagePresenter IFormatsWizardScenario.Current
		{
			get
			{
				switch (stage)
				{
					case 0: return jsonPage;
					case 1: return identityPage;
					case 2: return optionsPage;
					case 3: return savePage;
				}
				return null;
			}
		}
		WizardScenarioFlag IFormatsWizardScenario.Flags
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
		JsonBasedFormatPage.IPresenter jsonPage;
		FormatIdentityPage.IPresenter identityPage;
		FormatAdditionalOptionsPage.IPresenter optionsPage;
		SaveFormatPage.IPresenter savePage;
	};
};