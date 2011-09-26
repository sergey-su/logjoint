using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ChooseOperationPage : UserControl
	{
		IWizardScenarioHost host;

		public ChooseOperationPage(IWizardScenarioHost host)
		{
			InitializeComponent();
			this.host = host;
		}

		private void cloneRadioButton_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Clicks >= 2)
			{
				host.Next();
			}
		}

	}

	public class RootScenario: IFormatsWizardScenario
	{
		public RootScenario(IWizardScenarioHost host)
		{
			this.host = host;
			chooseOpPage = new ChooseOperationPage(host);
		}

		public bool Next()
		{
			if (currentScenario != null)
				return currentScenario.Next();

			if (chooseOpPage.importLog4NetRadioButton.Checked)
				currentScenario = importLog4Net ?? (importLog4Net = new ImportLog4NetScenario(host));
			else if (chooseOpPage.changeRadioButton.Checked)
				currentScenario = changeExistingFmt ?? (changeExistingFmt = new OperationOverExistingFormatScenario(host));
			else if (chooseOpPage.newREBasedFmtRadioButton.Checked)
				currentScenario = newReBasedFmt ?? (newReBasedFmt = new ModifyRegexBasedFormatScenario(host));

			return true;
		}
		public bool Prev()
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
		public Control Current
		{
			get { return currentScenario != null ? currentScenario.Current : chooseOpPage; }
		}
		public WizardScenarioFlag Flags
		{
			get	{ return currentScenario != null ? currentScenario.Flags : WizardScenarioFlag.NextIsActive; }
		}

		IWizardScenarioHost host;

		ChooseOperationPage chooseOpPage;
		IFormatsWizardScenario currentScenario;

		IFormatsWizardScenario changeExistingFmt;
		IFormatsWizardScenario importLog4Net;
		IFormatsWizardScenario newReBasedFmt;
	};
}
