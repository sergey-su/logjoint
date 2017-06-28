using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using LogJoint.Extensibility;
using LogJoint.Chromium;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		IPostprocessorsRegistry postprocessorsRegistry;

		public Plugin()
		{
		}

		public override void Init(IApplication app)
		{
			app.Model.Postprocessing.TimeSeriesTypes.DefaultTimeSeriesTypesAssembly = typeof(Chromium.TimeSeries.PostprocessorsFactory).Assembly;

			postprocessorsRegistry = new PostprocessorsInitialilizer(
				app.Model.Postprocessing.PostprocessorsManager, 
				app.Model.UserDefinedFormatsManager, 
				new Chromium.StateInspector.PostprocessorsFactory(),
				new Chromium.TimeSeries.PostprocessorsFactory(app.Model.Postprocessing.TimeSeriesTypes)
			);
		}


		public override void Dispose()
		{
		}
	}
}
