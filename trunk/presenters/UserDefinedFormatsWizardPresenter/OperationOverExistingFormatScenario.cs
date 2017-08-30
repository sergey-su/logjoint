using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class OperationOverExistingFormatScenario : IFormatsWizardScenario
	{
		public OperationOverExistingFormatScenario(IWizardScenarioHost host, IObjectFactory fac)
		{
			this.host = host;
			this.fac = fac;
			this.choosePage = fac.CreateChooseExistingFormatPage(host);
		}

		bool IFormatsWizardScenario.Next()
		{
			if (currentScenario != null)
				return currentScenario.Next();

			if (!choosePage.ValidateInput())
				return false;

			if (choosePage.SelectedOption == ChooseExistingFormatPage.ControlId.Delete)
			{
				currentScenario = deleteScenario ?? (deleteScenario = fac.CreateDeleteFormatScenario(host));
				deleteScenario.SetCurrentFormat(choosePage.SelectedFormat);
			}
			else if (choosePage.SelectedOption == ChooseExistingFormatPage.ControlId.Change)
			{
				if (choosePage.SelectedFormat is RegularGrammar.UserDefinedFormatFactory)
				{
					currentScenario = modifyReScenario ?? (modifyReScenario = fac.CreateModifyRegexBasedFormatScenario(host));
					modifyReScenario.SetCurrentFormat(choosePage.SelectedFormat);
				}
				else if (choosePage.SelectedFormat is XmlFormat.UserDefinedFormatFactory)
				{
					currentScenario = modifyXmlScenario ?? (modifyXmlScenario = fac.CreateModifyXmlBasedFormatScenario(host));
					modifyXmlScenario.SetCurrentFormat(choosePage.SelectedFormat);
				}
			} 

			return true;
		}

		bool IFormatsWizardScenario.Prev()
		{
			if (currentScenario != null)
			{
				if (!currentScenario.Prev())
					currentScenario = null;
				return true;
			}
			else
			{
				return false;
			}
		}

		void IFormatsWizardScenario.SetCurrentFormat(IUserDefinedFactory udf)
		{
		}

		IWizardPagePresenter IFormatsWizardScenario.Current
		{
			get { return currentScenario != null ? currentScenario.Current : choosePage; }
		}

		WizardScenarioFlag IFormatsWizardScenario.Flags
		{
			get { return currentScenario != null ? currentScenario.Flags : WizardScenarioFlag.NextIsActive | WizardScenarioFlag.BackIsActive; }
		}

		IWizardScenarioHost host;
		IObjectFactory fac;
		ChooseExistingFormatPage.IPresenter choosePage;
		IFormatsWizardScenario currentScenario;
		IFormatsWizardScenario deleteScenario;
		IFormatsWizardScenario modifyReScenario;
		IFormatsWizardScenario modifyXmlScenario;
	};
};