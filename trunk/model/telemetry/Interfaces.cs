using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace LogJoint.Telemetry
{
	public interface ITelemetryCollector
	{
		void ReportException(Exception e, string context);
		void ReportUsedFeature(string featureId, IEnumerable<KeyValuePair<string, int>> subFeaturesUseCounters = null);
	};

	public interface ITelemetryUploader
	{
		bool IsConfigured { get; }
		Task<TelemetryUploadResult> Upload(
			DateTime recordTimestamp,
			string recordId,
			Dictionary<string, string> fields,
			CancellationToken cancellation
		);
	};

	public enum TelemetryUploadResult
	{
		Success,
		Duplicate,
		Failure
	};
}
