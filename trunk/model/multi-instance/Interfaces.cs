using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint.MultiInstance
{
	public interface IInstancesCounter
	{
		bool IsPrimaryInstance { get; }
		string MutualExecutionKey { get; }
		int Count { get; }
	};
}
