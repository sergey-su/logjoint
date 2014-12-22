using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace LogJoint
{
	public class TimelineExtensions : ITimelineExtensions
	{
		public TimelineExtensions()
		{
		}

		string ITimelineExtensions.AddLifetimeBar(LifetimeBar bar)
		{
			string newId = string.Format("lifetimeBar#{0}", ++lastId);
			lifetimeBars.Add(newId, bar);
			return newId;
		}

		void ITimelineExtensions.RemoveLifetimeBar(string id)
		{
			lifetimeBars.Remove(id);
		}

		int lastId = 0;
		readonly Dictionary<string, LifetimeBar> lifetimeBars = new Dictionary<string,LifetimeBar>();
	}
}
