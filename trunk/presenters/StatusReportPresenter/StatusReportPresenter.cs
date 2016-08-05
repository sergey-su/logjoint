using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly List<StatusPopup> shownReports = new List<StatusPopup>();
		readonly IView view;

		public Presenter(IView view, IHeartBeatTimer heartbeatTimer)
		{
			this.view = view;
			this.view.SetViewEvents(this);

			heartbeatTimer.OnTimer += (s, e) => Timeslice();
		}

		IReport IPresenter.CreateNewStatusReport()
		{
			return new StatusPopup(this, view);
		}

		void IPresenter.CancelActiveStatus()
		{
			CancelActiveStatusInternal();
		}

		void IViewEvents.OnCancelLongRunningProcessButtonClicked()
		{
			CancelActiveStatusInternal();
		}

		void CancelActiveStatusInternal()
		{
			if (ActiveReport != null)
				ActiveReport.Cancel();
		}

		StatusPopup ActiveReport
		{
			get { return shownReports.LastOrDefault(); }
		}

		void Timeslice()
		{
			if(shownReports.Count > 0)
				foreach (var r in shownReports.ToArray())
					r.AutoHideIfItIsTime();
		}

		internal void ReportsTransaction(Action<List<StatusPopup>> body, bool allowReactivation)
		{
			var oldActive = ActiveReport;
			body(shownReports);
			var newActive = ActiveReport;
			if (newActive == oldActive && !allowReactivation)
				return;
			if (oldActive != null)
				oldActive.Deactivate();
			if (newActive != null)
				newActive.Activate();
		}
	}

};