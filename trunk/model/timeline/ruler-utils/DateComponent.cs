using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public enum DateComponent
	{
		None,
		Year,
		Month,
		Day,
		Hour,
		Minute,
		Seconds,
		Milliseconds
	};
}
