using System;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard
{
    public class ImportLog4NetScenario : IFormatsWizardScenario
    {
        XmlDocument doc;
        IWizardScenarioHost host;

        public ImportLog4NetScenario(IWizardScenarioHost host, IFactory fac)
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

            importPage = fac.CreateImportLog4NetPage(host);
            identityPage = fac.CreateFormatIdentityPage(host, newFormatMode: true);
            identityPage.SetFormatRoot(doc.DocumentElement);
            optionsPage = fac.CreateFormatAdditionalOptionsPage(host);
            optionsPage.SetFormatRoot(doc.SelectSingleNode("format/regular-grammar"));
            savePage = fac.CreateSaveFormatPage(host, newFormatMode: false);
            savePage.SetDocument(doc);
        }

        bool IFormatsWizardScenario.Next()
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

        bool IFormatsWizardScenario.Prev()
        {
            if (stage == 0)
                return false;
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
                        return identityPage;
                    case 2:
                        return optionsPage;
                    case 3:
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
                if (stage == 3)
                    f |= WizardScenarioFlag.NextIsFinish;
                return f;
            }
        }

        int stage;
        ImportLog4NetPage.IPresenter importPage;
        FormatIdentityPage.IPresenter identityPage;
        FormatAdditionalOptionsPage.IPresenter optionsPage;
        SaveFormatPage.IPresenter savePage;
    };
};