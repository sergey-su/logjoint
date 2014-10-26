using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace LogJoint.UI
{
	class StatusPopup: IStatusReport
	{
		StatusPopupsManager owner;
		int ticksWhenAutoHideStarted;

		public StatusPopup(StatusPopupsManager owner)
		{
			this.owner = owner;
		}

		#region IStatusReport Members

		public void ShowStatusText(string text, bool autoHide)
		{
			ShowCore("", Enumerable.Repeat(new StatusMessagePart(text), 1), autoHide, false);
		}

		public void ShowStatusPopup(string caption, IEnumerable<StatusMessagePart> parts, bool autoHide)
		{
			ShowCore(caption, parts, autoHide, true);
		}

		public void ShowStatusPopup(string caption, string text, bool autoHide)
		{
			ShowCore(caption, Enumerable.Repeat(new StatusMessagePart(text), 1), autoHide, true);
		}
			
		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (IsAutoHide)
			{
				owner.autoHideStatusReport = null;
			}
			if (IsActive)
			{
				owner.toolStripStatusLabel.Text = "";
				owner.infoPopup.HidePopup();
				owner.activeStatusReport = null;
			}
		}

		#endregion

		void ShowCore(string caption, IEnumerable<StatusMessagePart> parts, bool autoHide, bool popup)
		{
			if (IsActive)
			{
				if (popup)
				{
					var popupParts = parts.Select(part =>
					{
						var link = part as StatusMessageLink;
						if (link != null)
							return new InfoPopupControl.Link(link.Text, link.Click);
						else
							return new InfoPopupControl.MessagePart(part.Text);
					});
					owner.infoPopup.ShowPopup(caption, popupParts, new Point(owner.appWindow.ClientSize.Width - 20, owner.appWindow.ClientSize.Height));
					owner.toolStripStatusLabel.Text = "";
				}
				else
				{
					string statusText = parts.First().Text;
					owner.toolStripStatusLabel.Text = statusText;
					owner.infoPopup.HidePopup();
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
				Dispose();
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
