using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ManageFormatsWizard : Form, IWizardScenarioHost
	{
		Control currentContent;
		IFormatsWizardScenario scenario;
		IModel model;

		public ManageFormatsWizard(IModel model)
		{
			this.model = model;
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
			UpdateView();
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
			Back();
		}

		private void nextButton_Click(object sender, EventArgs e)
		{
			Next();
		}

		public void UpdateView()
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

		public void Next()
		{
			if (!ValidateSwitch(true))
				return;
			scenario.Next();
			UpdatePageContent();
		}

		public void Back()
		{
			if (!ValidateSwitch(false))
				return;
			scenario.Prev();
			UpdatePageContent();
		}

		public void Finish()
		{
			Close();
		}

		public IModel Model { get { return model; } }

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}

	public interface IWizardScenarioHost
	{
		IModel Model { get; }

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