using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public struct RulerMark
	{
		public readonly DateTime Time;
		public readonly bool IsMajor;
		public readonly DateComponent Component;

		public RulerMark(DateTime d, bool isMajor, DateComponent comp)
		{
			Time = d;
			IsMajor = isMajor;
			Component = comp;
		}
	};

}
