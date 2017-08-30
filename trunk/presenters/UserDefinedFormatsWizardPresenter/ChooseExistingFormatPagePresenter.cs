using System.Linq;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.FormatsWizard.ChooseExistingFormatPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly IUserDefinedFormatsManager udf;
		readonly IAlertPopup alerts;
		readonly List<IUserDefinedFactory> list = new List<IUserDefinedFactory>();

		public Presenter(
			IView view, 
			IWizardScenarioHost host,
			IUserDefinedFormatsManager udf,
			IAlertPopup alerts
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.udf = udf;
			this.alerts = alerts;

			LoadFormatsList();
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			return true;
		}

		object IWizardPagePresenter.ViewObject => view;

		ControlId IPresenter.SelectedOption => view.SelectedOption;

		IUserDefinedFactory IPresenter.SelectedFormat => GetSelectedFormat();

		bool IPresenter.ValidateInput()
		{
			string msg = ValidateInputInternal();
			if (msg == null)
				return true;
			alerts.ShowPopup("Validation", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
			return false;
		}

		void LoadFormatsList()
		{
			list.Clear();
			list.AddRange(udf.Items);
			view.SetFormatsListBoxItems(list.Select(i => i.ToString()).ToArray());
		}

		IUserDefinedFactory GetSelectedFormat()
		{
			return list.ElementAtOrDefault(view.SelectedFormatsListBoxItem);
		}

		string ValidateInputInternal()
		{
			if (GetSelectedFormat() == null)
			{
				return "Select a format";
			}
			if (view.SelectedOption == ControlId.None)
			{
				return "Select action to perform";
			}
			return null;
		}

		void TryToGoNext()
		{
			if (ValidateInputInternal() != null)
				return;
			host.Next();
		}

		void IViewEvents.OnControlDblClicked()
		{
			TryToGoNext();
		}
	};
};