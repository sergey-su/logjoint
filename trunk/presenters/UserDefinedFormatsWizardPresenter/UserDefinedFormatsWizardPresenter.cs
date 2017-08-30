using System;

namespace LogJoint.UI.Presenters.FormatsWizard
{

	public class Presenter : IPresenter, IViewEvents, IWizardScenarioHost
	{
		readonly Action reset;
		Lazy<IView> view;
		IFormatsWizardScenario scenario;
		IWizardPagePresenter currentContent;

		public Presenter(IObjectFactory fac)
		{
			reset = () =>
			{
				this.view = new Lazy<IView>(() =>
				{
					var ret = fac.CreateWizardView();
					ret.SetEventsHandler(this);
					return ret;
				});
				this.scenario = fac.CreateRootScenario(this);
			};
		}

		void IPresenter.ShowDialog()
		{
			reset();
			UpdatePageContent();
			view.Value.ShowDialog();
		}

		void IViewEvents.OnBackClicked()
		{
			((IWizardScenarioHost)this).Back();
		}

		void IViewEvents.OnNextClicked()
		{
			((IWizardScenarioHost)this).Next();
		}

		void IViewEvents.OnCloseClicked()
		{
			view.Value.CloseDialog();
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
			view.Value.CloseDialog();
		}

		bool ValidateSwitch(bool movingForward)
		{
			var wp = currentContent;
			if (wp != null && !wp.ExitPage(movingForward))
				return false;
			return true;
		}

		void UpdateView()
		{
			WizardScenarioFlag f = scenario.Flags;
			view.Value.SetControls(
				backEnabled: (f & WizardScenarioFlag.BackIsActive) != 0,
				backText: "<<Back",
				nextEnabled: (f & WizardScenarioFlag.NextIsActive) != 0,
				nextText: (f & WizardScenarioFlag.NextIsFinish) != 0 ? "Finish" : "Next>>"
			);
		}

		void UpdatePageContent()
		{
			var content = scenario.Current;

			if (currentContent != null)
			{
				view.Value.HidePage(currentContent.ViewObject);
			}
			currentContent = content;
			if (currentContent != null)
			{
				view.Value.ShowPage(currentContent.ViewObject);
			}
			UpdateView();
		}
	};
};