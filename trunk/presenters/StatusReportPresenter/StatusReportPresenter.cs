using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	public class Presenter : IPresenter
	{
		internal StatusPopup activeStatusReport;
		internal StatusPopup autoHideStatusReport;
		internal IView view;

		public Presenter(IView view, IHeartBeatTimer heartbeatTimer)
		{
			this.view = view;

			heartbeatTimer.OnTimer += (s, e) => Timeslice();
		}

		IReport IPresenter.CreateNewStatusReport()
		{
			if (activeStatusReport != null)
				activeStatusReport.Dispose();
			activeStatusReport = new StatusPopup(this);
			return activeStatusReport;
		}

		void Timeslice()
		{
			if (autoHideStatusReport != null)
			{
				autoHideStatusReport.AutoHideIfItIsTime();
			}
		}
	}

};