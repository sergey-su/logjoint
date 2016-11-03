using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.StatusReports
{
	class StatusPopup: IReport
	{
		Presenter owner;
		IView view;
		Action cancellationHandler;
		string caption;
		List<MessagePart> parts;
		bool popup;
		int? ticksWhenAutoHideStarted;

		public StatusPopup(Presenter owner, IView view)
		{
			this.owner = owner;
			this.view = view;
		}

		void IReport.SetCancellationHandler(Action handler)
		{
			this.cancellationHandler = handler;
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
			owner.ReportsTransaction(shownReports => shownReports.Remove(this), false, popup);
		}

		void ShowCore(string caption, IEnumerable<MessagePart> parts, bool autoHide, bool popup)
		{
			this.ticksWhenAutoHideStarted = autoHide ? Environment.TickCount : new int?();
			this.caption = caption;
			this.parts = parts.ToList();
			this.popup = popup;

			owner.ReportsTransaction(shownReports =>
			{
				shownReports.Remove(this); // remove report if it was shown earlier
				shownReports.Add(this); // add to top - become active
			}, true, popup);
		}

		internal void Cancel()
		{
			if (cancellationHandler != null)
				cancellationHandler();
		}

		internal void Activate()
		{
			if (popup)
			{
				view.ShowPopup(caption, parts);
			}
			else
			{
				string statusText = parts.First().Text;
				view.SetStatusText(statusText);
				if (cancellationHandler != null)
				{
					view.SetCancelLongRunningControlsVisibility(true);
				}
			}
		}

		internal void Deactivate()
		{
			if (popup)
				view.HidePopup();
			else
				view.SetStatusText("");
			if (cancellationHandler != null)
				view.SetCancelLongRunningControlsVisibility(false);
			
		}

		internal void AutoHideIfItIsTime()
		{
			if (ticksWhenAutoHideStarted != null && Environment.TickCount - this.ticksWhenAutoHideStarted > 1000 * 3)
			{
				this.Dispose();
			}
		}
	}
}