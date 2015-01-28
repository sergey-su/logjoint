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
			var breif = new StringBuilder();
			string details = null;
			bool canCheckNow = false;
			switch (model.State)
			{
				case AutoUpdateState.Disabled:
				case AutoUpdateState.Inactive:
					breif.Append("NA");
					break;
				case AutoUpdateState.WaitingRestart:
					breif.Append("New update was downloaded. Restart LogJoint to apply it.");
					break;
				case AutoUpdateState.Checking:
					breif.Append("checking for new update...");
					break;
				case AutoUpdateState.Idle:
					var lastCheckResult = model.LastUpdateCheckResult;
					if (lastCheckResult == null)
					{
						breif.Append("never checked for update");
					}
					else
					{
						if (lastCheckResult.ErrorMessage == null)
						{
							breif.AppendFormat("You're up to date as of {0}", lastCheckResult.When.ToLocalTime());
						}
						else
						{
							breif.AppendFormat("update at {0} failed.", lastCheckResult.When.ToLocalTime());
							details = lastCheckResult.ErrorMessage;
						}
					}
					canCheckNow = true;
					break;
				case AutoUpdateState.Failed:
					breif.Append("failure");
					break;
				default:
					breif.Append("?");
					break;
			}
			view.SetLastUpdateCheckInfo(breif.ToString(), details);
			view.SetCheckNowButtonAvailability(canCheckNow);
		}


		readonly IAutoUpdater model;
		readonly IView view;
		readonly IGlobalSettingsAccessor settingsAccessor;

		#endregion
	};
};