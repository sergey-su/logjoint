namespace LogJoint.Extensibility
{
	public interface IModel: LogJoint.IModel // todo: remove in favor of SDK's one
	{
		IChangeNotification ChangeNotification { get; }
		Telemetry.ITelemetryCollector Telemetry { get; }
		IHeartBeatTimer Heartbeat { get; }
		ILogSourcesController LogSourcesController { get; }
		IShutdown Shutdown { get; }
		WebBrowserDownloader.IDownloader WebBrowserDownloader { get; }
		AppLaunch.ICommandLineHandler CommandLineHandler { get; }
		new IPostprocessingModel Postprocessing { get; }
	};

	public interface IPostprocessingModel: LogJoint.IPostprocessingModel // todo: remove in favor of SDK's one
	{
		Postprocessing.IUserNamesProvider ShortNames { get; }
		Analytics.TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; }
		Postprocessing.IAggregatingLogSourceNamesProvider LogSourceNamesProvider { get; }
	};
}
