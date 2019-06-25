using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class DeleteFormatScenario : IFormatsWizardScenario
	{
		readonly IWizardScenarioHost host;
		readonly IAlertPopup alerts;
		readonly FormatDeleteConfirmPage.IPresenter confirmPage;
		IUserDefinedFactory format;


		public DeleteFormatScenario(IWizardScenarioHost host, IAlertPopup alerts, IFactory fac)
		{
			this.host = host;
			this.alerts = alerts;
			this.confirmPage = fac.CreateFormatDeleteConfirmPage(host);
		}

		bool IFormatsWizardScenario.Next()
		{
			try
			{
				System.IO.File.Delete(format.Location);
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Error", e.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
				return false;
			}
			host.Finish();
			return false;
		}

		bool IFormatsWizardScenario.Prev()
		{
			return false;
		}

		void IFormatsWizardScenario.SetCurrentFormat(IUserDefinedFactory udf)
		{
			this.format = udf;
			confirmPage.UpdateView(format);
		}

		IWizardPagePresenter IFormatsWizardScenario.Current
		{
			get { return confirmPage; }
		}

		WizardScenarioFlag IFormatsWizardScenario.Flags
		{
			get { return WizardScenarioFlag.NextIsActive | WizardScenarioFlag.NextIsFinish | WizardScenarioFlag.BackIsActive; }
		}
	}
};