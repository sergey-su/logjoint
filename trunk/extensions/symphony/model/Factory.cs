using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Symphony
{
	public class ModelObjects
	{
		public IPostprocessorsRegistry PostprocessorsRegistry { get; internal set; }
		public SpringServiceLog.IPreprocessingStepsFactory BackendLogsPreprocessingStepsFactory { get; internal set; }
	};

	public class Factory
	{
		public static ModelObjects Create(LogJoint.IModel appModel)
		{
			appModel.Postprocessing.TimeSeries.RegisterTimeSeriesTypesAssembly(typeof(TimeSeries.PostprocessorsFactory).Assembly);

			StateInspector.IPostprocessorsFactory statePostprocessors = new StateInspector.PostprocessorsFactory(
				appModel.TempFilesManager,
				appModel.Postprocessing
			);

			TimeSeries.IPostprocessorsFactory timeSeriesPostprocessors = new TimeSeries.PostprocessorsFactory(
				appModel.Postprocessing
			);

			Timeline.IPostprocessorsFactory timelinePostprocessors = new Timeline.PostprocessorsFactory(
				appModel.TempFilesManager,
				appModel.Postprocessing
			);

			SequenceDiagram.IPostprocessorsFactory sequenceDiagramPostprocessors = new SequenceDiagram.PostprocessorsFactory(
				appModel.Postprocessing
			);

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				appModel.Postprocessing.Manager,
				appModel.UserDefinedFormatsManager,
				statePostprocessors,
				timeSeriesPostprocessors,
				new Correlator.PostprocessorsFactory(appModel),
				timelinePostprocessors,
				sequenceDiagramPostprocessors
			);

			var chromiumPlugin = appModel.PluginsManager.Get<Chromium.IPluginModel>();
			if (chromiumPlugin != null)
			{
				chromiumPlugin.RegisterSource(statePostprocessors.CreateChromeDebugSourceFactory());
				chromiumPlugin.RegisterSource(timeSeriesPostprocessors.CreateChromeDebugSourceFactory());
				chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDebugLogEventsSourceFactory());
				chromiumPlugin.RegisterSource(timelinePostprocessors.CreateChromeDriverEventsSourceFactory());
				chromiumPlugin.RegisterSource(sequenceDiagramPostprocessors.CreateChromeDebugLogEventsSourceFactory());
			}

			appModel.Preprocessing.ExtensionsRegistry.AddLogDownloaderRule(
				new Uri("https://perzoinc.atlassian.net/secure/attachment/"),
				Preprocessing.LogDownloaderRule.CreateBrowserDownloaderRule(new[] { "https://id.atlassian.com/login" })
			);

			SpringServiceLog.IPreprocessingStepsFactory backendLogsPreprocessingStepsFactory = null;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				backendLogsPreprocessingStepsFactory = new SpringServiceLog.PreprocessingStepsFactory(
					appModel.Preprocessing.StepsFactory,
					appModel.WebViewTools,
					appModel.ContentCache
				);
				appModel.Preprocessing.ExtensionsRegistry.Register(new SpringServiceLog.PreprocessingManagerExtension(
					backendLogsPreprocessingStepsFactory));
			}

			return new ModelObjects
			{
				PostprocessorsRegistry = postprocessorsRegistry,
				BackendLogsPreprocessingStepsFactory = backendLogsPreprocessingStepsFactory
			};
		}
	};
}
