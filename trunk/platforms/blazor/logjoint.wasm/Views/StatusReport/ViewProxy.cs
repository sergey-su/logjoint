using LogJoint.UI.Presenters.StatusReports;
using System.Collections.Generic;

namespace LogJoint.Wasm.UI
{
	public class StatusReportViewProxy : IView
	{
        void IView.SetViewEvents(IViewEvents viewEvents)
        {
        }

        void IView.SetStatusText(string value)
        {
        }

        void IView.HidePopup()
        {
        }

        void IView.ShowPopup(string caption, IEnumerable<MessagePart> parts)
        {
        }

        void IView.SetCancelLongRunningControlsVisibility(bool value)
        {
        }
    }
}
