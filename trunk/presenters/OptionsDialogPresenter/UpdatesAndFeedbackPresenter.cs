using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Settings;
using LogJoint.AutoUpdate;

namespace LogJoint.UI.Presenters.Options.UpdatesAndFeedback
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IAutoUpdater model,
			IGlobalSettingsAccessor settingsAccessor,
			IView view)
		{
			this.model = model;
			this.view = view;
			this.settingsAccessor = settingsAccessor;

			view.SetPresenter(this);

			UpdateAutomaticUpdatesView();
			model.Changed += (s, e) => UpdateAutomaticUpdatesView();
		}

		bool IPresenter.Apply()
		{
			return true;
		}

		bool IPresenter.IsAvailable
		{
			get { return model.State != AutoUpdateState.Disabled; }
		}

		void IViewEvents.OnCheckUpdateNowClicked()
		{
			model.CheckNow();
		}

		#region Implementation

		void UpdateAutomaticUpdatesView()
		{
			var brief = new StringBuilder();
			string details = null;
			bool canCheckNow = false;
			switch (model.State)
			{
				case AutoUpdateState.Disabled:
				case AutoUpdateState.Inactive:
					brief.Append("NA");
					break;
				case AutoUpdateState.WaitingRestart:
					brief.Append("New update was downloaded. Restart LogJoint to apply it.");
					break;
				case AutoUpdateState.Checking:
					brief.Append("checking for new update...");
					break;
				case AutoUpdateState.Idle:
					var lastCheckResult = model.LastUpdateCheckResult;
					if (lastCheckResult == null)
					{
						brief.Append("never checked for update");
					}
					else
					{
						if (lastCheckResult.ErrorMessage == null)
						{
							brief.AppendFormat("You're up to date as of {0}", lastCheckResult.When.ToLocalTime());
						}
						else
						{
							brief.AppendFormat("update at {0} failed.", lastCheckResult.When.ToLocalTime());
							details = lastCheckResult.ErrorMessage;
						}
					}
					canCheckNow = true;
					break;
				case AutoUpdateState.Failed:
					brief.Append("failure");
					break;
				case AutoUpdateState.FailedDueToBadInstallationDirectory:
					brief.Append("bad intallation directory detected.");
					details = string.Format(
						@"For automtaic updates to work LogJoint must be installed"+
						" to a directory allowed to be written by the current user ({0}).",
						Environment.UserName
					);
					break;
				default:
					brief.Append("?");
					break;
			}
			view.SetLastUpdateCheckInfo(brief.ToString(), details);
			view.SetCheckNowButtonAvailability(canCheckNow);
		}


		readonly IAutoUpdater model;
		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;

		#endregion
	};
};