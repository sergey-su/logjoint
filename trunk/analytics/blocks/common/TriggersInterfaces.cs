using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics
{
	public interface ITriggerStreamPosition
	{
		long StreamPosition { get; }
	}

	public interface ITriggerTime
	{
		DateTime Timestamp { get; }
	}

	public interface IOrderedTrigger
	{
		int Index { get; }
	}

	public interface ITriggerThread
	{
		string ThreadId { get; }
	};
}