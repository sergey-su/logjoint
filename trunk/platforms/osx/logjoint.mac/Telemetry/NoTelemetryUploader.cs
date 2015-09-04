using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Telemetry
{
	class NoTelemetryUploader: ITelemetryUploader
	{
		bool ITelemetryUploader.IsConfigured
		{
			get { return false; }
		}

		async Task<TelemetryUploadResult> ITelemetryUploader.Upload(DateTime recordTimestamp, string recordId, Dictionary<string, string> fields, CancellationToken cancellation)
		{
			throw new InvalidOperationException("no telemetry allowed");
		}
	}
}
