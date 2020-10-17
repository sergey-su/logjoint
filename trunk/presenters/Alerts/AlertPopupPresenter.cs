using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters
{
	class AlertPopup: IAlertPopup
	{
		AlertFlags IAlertPopup.ShowPopup(string caption, string text, AlertFlags flags) =>
			throw new NotImplementedException("sync popups not supported");

		Task<AlertFlags> IAlertPopup.ShowPopupAsync(string caption, string text, AlertFlags flags)
		{
		}
	};
}