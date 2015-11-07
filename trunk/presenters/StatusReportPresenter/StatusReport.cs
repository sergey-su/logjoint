using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	class StatusPopup: IReport
	{
		Presenter owner;
		int ticksWhenAutoHideStarted;

		public StatusPopup(Presenter owner)
		{
			this.owner = owner;
		}

		void IReport.ShowStatusText(string text, bool autoHide)
		{
			ShowCore("", Enumerable.Repeat(new MessagePart(text), 1), autoHide, false);
		}

		void IReport.ShowStatusPopup(string caption, IEnumerable<MessagePart> parts, bool autoHide)
		{
			ShowCore(caption, parts, autoHide, true);
		}

		void IReport.ShowStatusPopup(string caption, string text, bool autoHide)
		{
			ShowCore(caption, Enumerable.Repeat(new MessagePart(text), 1), autoHide, true);
		}

		public void Dispose()
		{
			if (IsAutoHide)
			{
				owner.autoHideStatusReport = null;
			}
			if (IsActive)
			{
				owner.view.SetStatusText("");
				owner.view.HidePopup();
				owner.activeStatusReport = null;
			}
		}

		void ShowCore(string caption, IEnumerable<MessagePart> parts, bool autoHide, bool popup)
		{
			if (IsActive)
			{
				if (popup)
				{
					owner.view.ShowPopup(caption, parts);
					owner.view.SetStatusText("");
				}
				else
				{
					string statusText = parts.First().Text;
					owner.view.SetStatusText(statusText);
					owner.view.HidePopup();
				}
				if (autoHide)
				{
					ticksWhenAutoHideStarted = Environment.TickCount;
					owner.autoHideStatusReport = this;
				}
				else
				{
					owner.autoHideStatusReport = null;
				}
			}
		}

		public void AutoHideIfItIsTime()
		{
			if (Environment.TickCount - this.ticksWhenAutoHideStarted > 1000 * 3)
				this.Dispose();
		}

		bool IsActive
		{
			get { return owner.activeStatusReport == this; }
		}
		bool IsAutoHide
		{
			get { return owner.autoHideStatusReport == this; }
		}
	}
}