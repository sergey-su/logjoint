using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	public class DeleteFormatScenario : IFormatsWizardScenario
	{
		readonly IWizardScenarioHost host;
		readonly IAlertPopup alerts;
		readonly FormatDeleteConfirmPage.IPresenter confirmPage;
		IUserDefinedFactory format;


		public DeleteFormatScenario(IWizardScenarioHost host, IUserDefinedFactory format, IAlertPopup alerts, IObjectFactory fac)
		{
			this.host = host;
			this.alerts = alerts;
			this.format = format;
			this.confirmPage = fac.CreateFormatDeleteConfirmPage(host);
			confirmPage.UpdateView(format);
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