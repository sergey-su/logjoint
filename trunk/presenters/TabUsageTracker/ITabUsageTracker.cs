using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public interface ITabUsageTracker
	{
		void OnTabPressed();
		bool FocusRectIsRequired { get; }
	};
};