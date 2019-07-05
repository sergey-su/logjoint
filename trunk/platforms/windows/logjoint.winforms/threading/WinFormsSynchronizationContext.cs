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
				void createHandler(object s, EventArgs e)
				{
					isReady = true;
					Post(() => { });
				}
				form.HandleCreated += createHandler;
				void destroyHandler(object s, EventArgs e)
				{
					isReady = false;
				}
				form.HandleDestroyed += destroyHandler;
			}
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
