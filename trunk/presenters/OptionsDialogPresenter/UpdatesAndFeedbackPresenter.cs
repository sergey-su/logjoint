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
			var pres = model.GetPresentation(preferShortBrief: false);
			view.SetLastUpdateCheckInfo(pres.Brief, pres.Details);
			view.SetCheckNowButtonAvailability(pres.CanCheckNow);
		}

		readonly IAutoUpdater model;
		readonly IView view;

		#endregion
	};
};