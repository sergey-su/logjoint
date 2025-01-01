using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Telemetry
{
    public interface ITelemetryCollector
    {
        void ReportException(Exception e, string context);
        void ReportUsedFeature(string featureId, IEnumerable<KeyValuePair<string, int>> subFeaturesUseCounters = null);
        Task ReportIssue(string description);
    };

    public interface ITelemetryUploader
    {
        bool IsTelemetryConfigured { get; }
        bool IsIssuesReportingConfigured { get; }
        Task<TelemetryUploadResult> Upload(
            DateTime recordTimestamp,
            string recordId,
            Dictionary<string, string> fields,
            CancellationToken cancellation
        );
        Task<string> UploadIssueReport(
            Stream reportStream,
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
