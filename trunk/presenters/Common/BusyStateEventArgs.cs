using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters
{
	public class BusyStateEventArgs : EventArgs
	{
		readonly bool busyStateRequired;

		public bool BusyStateRequired
		{
			get { return busyStateRequired; }
		}

		public BusyStateEventArgs(bool busyStateRequired)
		{
			this.busyStateRequired = busyStateRequired;
		}
	};
};