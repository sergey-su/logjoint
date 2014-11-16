using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public interface IMessagesCollection
	{
		int Count { get; }
		IEnumerable<IndexedMessage> Forward(int begin, int end);
		IEnumerable<IndexedMessage> Reverse(int begin, int end);
	};

	public class TimeConstraintViolationException : InvalidOperationException
	{
		public TimeConstraintViolationException() :
			base("Time constraint violation.")
		{
		}
	};
}
