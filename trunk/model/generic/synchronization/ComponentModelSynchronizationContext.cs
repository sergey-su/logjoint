using System;
using System.ComponentModel;

namespace LogJoint
{
	public class ComponentModelSynchronizationContext : ISynchronizationContext
	{
		public ComponentModelSynchronizationContext(ISynchronizeInvoke impl, Func<bool> isReady = null)
		{
			this.impl = impl;
			this.isReady = isReady != null ? isReady : () => true;
		}

		public bool PostRequired 
		{
			get { return impl.InvokeRequired; } 
		}

		public void Post(Action action)
		{
			if (isReady())
				impl.BeginInvoke((Action)(() => action()), new object[0]);
		}

		readonly ISynchronizeInvoke impl;
		readonly Func<bool> isReady;
	}
}
