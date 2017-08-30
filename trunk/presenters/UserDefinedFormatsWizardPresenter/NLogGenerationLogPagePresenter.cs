using System;
using LogJoint.NLog;
using System.Text;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.FormatsWizard.NLogGenerationLogPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;

		public Presenter(
			IView view, 
			IWizardScenarioHost host
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			return true;
		}

		object IWizardPagePresenter.ViewObject => view;


		void IPresenter.UpdateView(string layout, ImportLog importLog)
		{
			string headerLabelValue;
			IconType headerIcon;
			if (importLog.HasErrors)
			{
				headerLabelValue = "LogJoint can not import your NLog layout. Check messages below.";
				headerIcon = IconType.ErrorIcon;
			}
			else if (importLog.HasWarnings)
			{
				headerLabelValue = "LogJoint imported your NLog layout with warnings. Check messages below.";
				headerIcon = IconType.WarningIcon;
			}
			else
			{
				headerLabelValue = null;
				headerIcon = IconType.None;
			}

			var messagesList = new List<MessagesListItem>();

			foreach (var message in importLog.Messages)
			{
				var linkLabel = new MessagesListItem()
				{
					Links = new List<Tuple<int, int, Action>>()
				};

				StringBuilder messageText = new StringBuilder();

				foreach (var fragment in message.Fragments)
				{
					if (messageText.Length > 0)
						messageText.Append(' ');
					var layoutSliceFragment = fragment as NLog.ImportLog.Message.LayoutSliceLink;
					if (layoutSliceFragment != null)
					{
						linkLabel.Links.Add(Tuple.Create(messageText.Length, layoutSliceFragment.Value.Length, (Action)(() =>
						{
							view.SelectLayoutTextRange(layoutSliceFragment.LayoutSliceStart, layoutSliceFragment.LayoutSliceEnd - layoutSliceFragment.LayoutSliceStart);
						})));
					}
					messageText.Append(fragment.Value);
				}

				linkLabel.Text = messageText.ToString();

				if (message.Severity == NLog.ImportLog.MessageSeverity.Error)
					linkLabel.Icon = IconType.ErrorIcon;
				else if (message.Severity == NLog.ImportLog.MessageSeverity.Warn)
					linkLabel.Icon = IconType.WarningIcon;
				else
					linkLabel.Icon = IconType.NeutralIcon;

				messagesList.Add(linkLabel);
			}

			view.Update(layout, headerLabelValue, headerIcon, messagesList.ToArray());
		}
	};
};