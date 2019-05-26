using System;
using System.Linq;

namespace LogJoint.Symphony
{
	public static class PluginInitializer
	{
		public static void Init(IApplication app)
		{
			StateInspector.IPostprocessorsFactory statePostprocessors = new StateInspector.PostprocessorsFactory(
				app.Model.TempFilesManager,
				app.Model.Postprocessing
			);

			TimeSeries.IPostprocessorsFactory timeSeriesPostprocessors = new TimeSeries.PostprocessorsFactory(
				app.Model.Postprocessing
			);

			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager, 
				app.Model.UserDefinedFormatsManager,
				statePostprocessors,
				timeSeriesPostprocessors,
				new Correlator.PostprocessorsFactory(app.Model),
				new Timeline.PostprocessorsFactory(app.Model.TempFilesManager, app.Model.Postprocessing),
				new SequenceDiagram.PostprocessorsFactory(app.Model.Postprocessing)
			);

			var chromiumPlugin = app.Model.PluginsManager.Get<Chromium.IPluginModel>();
			if (chromiumPlugin != null) // todo: plugin init order
			{
				chromiumPlugin.RegisterSource(statePostprocessors.CreateChromeDebugSourceFactory());
				chromiumPlugin.RegisterSource(timeSeriesPostprocessors.CreateChromeDebugSourceFactory());
			}
		}
	}
}
