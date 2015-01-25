using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading;

namespace LogJoint.AutoUpdate
{
	public class SemaphoreMutualExecutionCounter: IMutualExecutionCounter
	{
		readonly string semaphoreName = "LogJoint.AutoUpdate.Lock";
		Semaphore sema;

		public SemaphoreMutualExecutionCounter()
		{
		}
	
		void IMutualExecutionCounter.Add(out bool isFirst)
		{
			if (sema != null)
				throw new InvalidOperationException();
			sema = new Semaphore(0, 1000, semaphoreName, out isFirst);
		}

		void IMutualExecutionCounter.Release()
		{
			if (sema == null)
				throw new InvalidOperationException();
			sema.Release();
			sema = null;
		}

		string IMutualExecutionCounter.UpdaterArgumentValue { get { return semaphoreName; } }
		
	};
}
