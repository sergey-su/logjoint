using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public struct RulerIntervals
	{
		public readonly RulerInterval Major, Minor;
		public RulerIntervals(RulerInterval major, RulerInterval minor)
		{
			Major = major;
			Minor = minor;
		}
	};
}
