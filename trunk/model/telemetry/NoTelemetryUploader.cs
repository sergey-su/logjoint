using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Telemetry
{
	class NoTelemetryUploader: ITelemetryUploader
	{
		bool ITelemetryUploader.IsTelemetryConfigured => false;
		bool ITelemetryUploader.IsIssuesReportingConfigured => false;
		Task<TelemetryUploadResult> ITelemetryUploader.Upload(
			DateTime recordTimestamp,
			string recordId,
			Dictionary<string, string> fields,
			CancellationToken cancellation
		) => throw new InvalidOperationException("no telemetry allowed");
		Task<string> ITelemetryUploader.UploadIssueReport(
			Stream reportStream,
			CancellationToken cancellation
		) => throw new InvalidOperationException("no telemetry allowed");
	}
}
