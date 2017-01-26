namespace LogJoint.Telemetry
{
	class ConfiguredAzureTelemetryUploader: AzureTelemetryUploader
	{
		public ConfiguredAzureTelemetryUploader(): base(
			LogJoint.Properties.Settings.Default.TelemetryUrl,
			LogJoint.Properties.Settings.Default.IssuesUrl
		)
		{
		}
	}
}
