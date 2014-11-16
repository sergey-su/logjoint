using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public interface IViewUpdates
	{
		void RequestUpdate();
		void PostUpdateToUIDispatcherQueue();
	};
};