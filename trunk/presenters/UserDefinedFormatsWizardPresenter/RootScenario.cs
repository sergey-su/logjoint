using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	internal class RootScenario : IFormatsWizardScenario
	{
		public RootScenario(IWizardScenarioHost host, IObjectFactory fac)
		{
			this.host = host;
			this.fac = fac;
			chooseOpPage = fac.CreateChooseOperationPage(host);
		}

		bool IFormatsWizardScenario.Next()
		{
			if (currentScenario != null)
				return currentScenario.Next();

			var selectedControl = chooseOpPage.SelectedControl;
			if (selectedControl == ChooseOperationPage.ControlId.ImportLog4NetButton)
				currentScenario = importLog4Net ?? (importLog4Net = fac.CreateImportLog4NetScenario(host));
			else if (selectedControl == ChooseOperationPage.ControlId.ImportNLogButton)
				currentScenario = importNLog ?? (importNLog = fac.CreateImportNLogScenario(host));
			else if (selectedControl == ChooseOperationPage.ControlId.ChangeFormatButton)
				currentScenario = changeExistingFmt ?? (changeExistingFmt = fac.CreateOperationOverExistingFormatScenario(host));
			else if (selectedControl == ChooseOperationPage.ControlId.NewREBasedButton)
				currentScenario = newReBasedFmt ?? (newReBasedFmt = fac.CreateModifyRegexBasedFormatScenario(host));

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
			get { return currentScenario != null ? currentScenario.Current : chooseOpPage; }
		}

		WizardScenarioFlag IFormatsWizardScenario.Flags
		{
			get { return currentScenario != null ? currentScenario.Flags : WizardScenarioFlag.NextIsActive; }
		}

		IWizardScenarioHost host;
		IObjectFactory fac;

		ChooseOperationPage.IPresenter chooseOpPage;
		IFormatsWizardScenario currentScenario;

		IFormatsWizardScenario changeExistingFmt;
		IFormatsWizardScenario importLog4Net;
		IFormatsWizardScenario importNLog;
		IFormatsWizardScenario newReBasedFmt;
	};
};