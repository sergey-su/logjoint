using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class ManualSynchronizationContext : ISynchronizationContext
	{
		Queue<Action> actions = new Queue<Action>();

		void ISynchronizationContext.Post(Action action)
		{
			actions.Enqueue(action);
		}

		public void Deplete()
		{
			while (actions.Count > 0)
				actions.Dequeue()();
		}
	};
}
