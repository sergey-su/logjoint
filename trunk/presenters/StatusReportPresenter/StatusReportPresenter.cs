using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly List<StatusPopup> shownPopups = new List<StatusPopup>();
		readonly List<StatusPopup> shownTexts = new List<StatusPopup>();
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
			var rpt = GetActiveReport(shownTexts);
			if (rpt != null)
				rpt.Cancel();
		}

		static StatusPopup GetActiveReport(List<StatusPopup> shownReports)
		{
			return shownReports.LastOrDefault();
		}

		void Timeslice()
		{
			if(shownPopups.Count > 0)
				foreach (var r in shownPopups.ToArray())
					r.AutoHideIfItIsTime();
		}

		internal void ReportsTransaction(Action<List<StatusPopup>> body, bool allowReactivation, bool isPopup)
		{
			var list = isPopup ? shownPopups : shownTexts;
			var oldActive = GetActiveReport(list);
			body(list);
			var newActive = GetActiveReport(list);
			if (newActive == oldActive && !allowReactivation)
				return;
			if (oldActive != null)
				oldActive.Deactivate();
			if (newActive != null)
				newActive.Activate();
		}
	}
};