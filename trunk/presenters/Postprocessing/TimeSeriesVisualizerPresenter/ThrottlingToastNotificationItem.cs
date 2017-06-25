using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public class ThrottlingToastNotificationItem: IToastNotificationItem
	{
		bool isActive;

		public ThrottlingToastNotificationItem()
		{
		}

		public event EventHandler<ItemChangeEventArgs> Changed;

		void IToastNotificationItem.PerformAction (string actionId)
		{
		}

		bool IToastNotificationItem.IsActive
		{
			get { return isActive; }
		}

		string IToastNotificationItem.Contents
		{
			get { return "Dense data is throttled on this view"; }
		}

		double? IToastNotificationItem.Progress
		{
			get { return null; }
		}

		public void Update(bool isActive)
		{
			if (this.isActive == isActive)
				return;
			this.isActive = isActive;
			Changed?.Invoke(this, new ItemChangeEventArgs(isUnsuppressingChange: false));
		}
	};
}
