using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint.MultiInstance
{
	public class DummyInstancesCounter : IInstancesCounter
	{
		bool IInstancesCounter.IsPrimaryInstance => true;

		string IInstancesCounter.MutualExecutionKey => "";

		int IInstancesCounter.Count => 1;
	};
}
