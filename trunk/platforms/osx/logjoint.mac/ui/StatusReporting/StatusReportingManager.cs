using System;
using LogJoint.UI.Presenters.StatusReports;

namespace LogJoint.UI
{
	// todo: it's a stub. implement me right.
	public class StatusReportingManager: IPresenter
	{
		public StatusReportingManager()
		{
		}

		#region IPresenter implementation

		IReport IPresenter.CreateNewStatusReport()
		{
			return new Report();
		}

		#endregion

		class Report: IReport
		{
			void IReport.ShowStatusPopup(string caption, string text, bool autoHide)
			{
			}
			void IReport.ShowStatusPopup(string caption, System.Collections.Generic.IEnumerable<MessagePart> parts, bool autoHide)
			{
			}
			void IReport.ShowStatusText(string text, bool autoHide)
			{
			}
			void IDisposable.Dispose()
			{
			}
		};
	}
}

