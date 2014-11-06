using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public class TabUsageTracker: ITabUsageTracker
	{
		bool ITabUsageTracker.FocusRectIsRequired
		{
			get
			{
				if (!lastTimeTabHasBeenUsed.HasValue)
					return false;
				if (DateTime.Now - lastTimeTabHasBeenUsed > TimeSpan.FromSeconds(10))
				{
					lastTimeTabHasBeenUsed = null;
					return false;
				}
				return true;
			}
		}

		void ITabUsageTracker.OnTabPressed()
		{
			this.lastTimeTabHasBeenUsed = DateTime.Now;
		}

		DateTime? lastTimeTabHasBeenUsed;
	};
};