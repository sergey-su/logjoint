using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public interface ITimelineExtensions
	{
		string AddLifetimeBar(LifetimeBar bar);
		void RemoveLifetimeBar(string id);
	};

	public struct LifetimeBar
	{
		public readonly DateRange Lifetime;
		public readonly string Label;
		public LifetimeBar(DateRange lifetime, string label)
		{
			Lifetime = lifetime;
			Label = label;
		}
	};
}
