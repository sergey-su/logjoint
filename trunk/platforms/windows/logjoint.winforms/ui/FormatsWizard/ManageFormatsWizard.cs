using System;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ManageFormatsWizard : Form, IWizardScenarioHost
	{
		Control currentContent;
		IFormatsWizardScenario scenario;
		ITempFilesManager tempFilesManager;
		ILogProviderFactoryRegistry logProviderFactoryRegistry;
		IUserDefinedFormatsManager userDefinedFormatsManager;
		Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory;
		Presenters.Help.IPresenter help;

		public ManageFormatsWizard(
			ITempFilesManager tempFilesManager,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory, 
			Presenters.Help.IPresenter help
		)
		{
			this.logViewerPresenterFactory = logViewerPresenterFactory;
			this.help = help;
			this.tempFilesManager = tempFilesManager;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.userDefinedFormatsManager = userDefinedFormatsManager;
			InitializeComponent();
			scenario = new RootScenario(this);
		}

		public void ExecuteWizard()
		{
			UpdatePageContent();
			ShowDialog();
		}

		void UpdatePageContent()
		{
			Control content = scenario.Current;

			if (currentContent != null)
			{
				currentContent.Visible = false;
			}
			currentContent = content;
			if (currentContent != null)
			{
				currentContent.Parent = containerPanel;
				currentContent.Dock = DockStyle.Fill;
				currentContent.Visible = true;
				currentContent.Focus();
			}
			((IWizardScenarioHost)this).UpdateView();
		}

		private void containerPanel_Paint(object sender, PaintEventArgs e)
		{
			ControlPaint.DrawBorder3D(e.Graphics, containerPanel.ClientRectangle, 
				Border3DStyle.Etched, Border3DSide.Bottom);
		}

		private void containerPanel_Resize(object sender, EventArgs e)
		{
			containerPanel.Invalidate();
		}

		private void backButton_Click(object sender, EventArgs e)
		{
			((IWizardScenarioHost)this).Back();
		}

		private void nextButton_Click(object sender, EventArgs e)
		{
			((IWizardScenarioHost)this).Next();
		}

		void IWizardScenarioHost.UpdateView()
		{
			WizardScenarioFlag f = scenario.Flags;
			backButton.Enabled = (f & WizardScenarioFlag.BackIsActive) != 0;
			backButton.Text = "<<Back";
			nextButton.Enabled = (f & WizardScenarioFlag.NextIsActive) != 0;
			if ((f & WizardScenarioFlag.NextIsFinish) != 0)
				nextButton.Text = "Finish";
			else
				nextButton.Text = "Next>>";
		}

		bool ValidateSwitch(bool movingForward)
		{
			IWizardPage wp = currentContent as IWizardPage;
			if (wp != null && !wp.ExitPage(movingForward))
				return false;
			return true;
		}

		void IWizardScenarioHost.Next()
		{
			if (!ValidateSwitch(true))
				return;
			scenario.Next();
			UpdatePageContent();
		}

		void IWizardScenarioHost.Back()
		{
			if (!ValidateSwitch(false))
				return;
			scenario.Prev();
			UpdatePageContent();
		}

		void IWizardScenarioHost.Finish()
		{
			Close();
		}

		Presenters.Help.IPresenter IWizardScenarioHost.Help { get { return help; } }

		Presenters.LogViewer.IPresenterFactory IWizardScenarioHost.LogViewerPresenterFactory { get { return logViewerPresenterFactory; } }

		ITempFilesManager IWizardScenarioHost.TempFilesManager { get { return tempFilesManager; } }

		ILogProviderFactoryRegistry IWizardScenarioHost.LogProviderFactoryRegistry { get { return logProviderFactoryRegistry; } }

		IUserDefinedFormatsManager IWizardScenarioHost.UserDefinedFormatsManager { get { return userDefinedFormatsManager;  } }

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}

	public interface IWizardScenarioHost
	{
		Presenters.LogViewer.IPresenterFactory LogViewerPresenterFactory { get; }
		Presenters.Help.IPresenter Help { get; }
		ITempFilesManager TempFilesManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }

		void UpdateView();
		void Next();
		void Back();
		void Finish();
	};

	[Flags]
	public enum WizardScenarioFlag
	{
		None = 0,
		BackIsActive = 1,
		NextIsActive = 2,
		NextIsFinish = 4
	};

	public interface IFormatsWizardScenario
	{
		bool Next();
		bool Prev();
		Control Current { get; }
		WizardScenarioFlag Flags { get; }
	};

	public interface IWizardPage
	{
		bool ExitPage(bool movingForward);
	};

}