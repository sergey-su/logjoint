using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public interface IModel
	{
		IInvokeSynchronization ModelThreadSynchronization { get; }
		Telemetry.ITelemetryCollector Telemetry { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }
	};
}
