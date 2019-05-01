using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows.Forms;

namespace LogJoint
{
	public class WinFormsSynchronizationContext : ISynchronizationContext
	{
		public WinFormsSynchronizationContext(Form form)
		{
			this.impl = form;
			this.isReady = form.IsHandleCreated;
			if (!isReady)
			{
				void handler(object s, EventArgs e)
				{
					isReady = true;
					form.HandleCreated -= handler;
					Post(() => { });
				}
				form.HandleCreated += handler;
			}
		}

		public bool PostRequired 
		{
			get { return impl.InvokeRequired; } 
		}

		public void Post(Action action)
		{
			if (isReady)
			{
				while (pending.TryDequeue(out var p))
					impl.BeginInvoke(p, emptyArgs);
				impl.BeginInvoke(action, emptyArgs);
			}
			else
			{
				pending.Enqueue(action);
			}
		}

		readonly ISynchronizeInvoke impl;
		bool isReady;
		readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
		readonly object[] emptyArgs = new object[0];
	}
}
