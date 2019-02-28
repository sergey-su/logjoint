using System;
using System.ComponentModel;

namespace LogJoint
{
	public class ComponentModelSynchronizationContext : ISynchronizationContext
	{
		public ComponentModelSynchronizationContext(ISynchronizeInvoke impl)
		{
			this.impl = impl;
		}

		public bool PostRequired 
		{
			get { return impl.InvokeRequired; } 
		}

		public void Post(Action action)
		{
			impl.BeginInvoke((Action)(() => action()), new object[0]);
		}

		readonly ISynchronizeInvoke impl;
	}
}
