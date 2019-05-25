using System;
using System.Linq;

namespace LogJoint.Symphony
{
	public class PluginInitializer
	{
		public static void Init(IApplication app)
		{
			IPostprocessorsRegistry postprocessorsRegistry = new PostprocessorsInitializer(
				app.Model.Postprocessing.Manager, 
				app.Model.UserDefinedFormatsManager, 
				new Symphony.StateInspector.PostprocessorsFactory(app.Model.TempFilesManager, app.Model.Postprocessing),
				new Symphony.TimeSeries.PostprocessorsFactory(),
				new Symphony.Correlator.PostprocessorsFactory(app.Model),
				new Symphony.Timeline.PostprocessorsFactory(app.Model.TempFilesManager, app.Model.Postprocessing),
				new Symphony.SequenceDiagram.PostprocessorsFactory(app.Model.Postprocessing)
			);
		}
	}
}
