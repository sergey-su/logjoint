using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public struct RulerInterval
	{
		public readonly TimeSpan Duration;
		public readonly DateComponent Component;
		public readonly int NonUniformComponentCount;
		public readonly bool IsHiddenWhenMajor;

		public RulerInterval(TimeSpan dur, int nonUniformComponentCount, DateComponent comp, bool isHiddenWhenMajor = false)
		{
			Duration = dur;
			Component = comp;
			NonUniformComponentCount = nonUniformComponentCount;
			IsHiddenWhenMajor = isHiddenWhenMajor;
		}
	};
}
