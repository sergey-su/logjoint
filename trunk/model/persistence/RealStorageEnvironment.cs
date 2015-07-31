using System;
using System.Threading.Tasks;

namespace LogJoint.Persistence.Implementation
{
	public class RealTimingAndThreading : ITimingAndThreading
	{
		DateTime ITimingAndThreading.Now
		{
			get { return DateTime.Now; }
		}

		Task ITimingAndThreading.StartTask(Action routine)
		{
			var t = new Task(routine);
			t.Start();
			return t;
		}
	};
}
