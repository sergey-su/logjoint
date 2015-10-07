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
	class ConfiguredAzureTelemetryUploader: AzureTelemetryUploader
	{
		public ConfiguredAzureTelemetryUploader(): base(LogJoint.Properties.Settings.Default.TelemetryUrl)
		{
		}
	}
}
