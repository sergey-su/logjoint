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
			postprocessorsRegistry = new PostprocessorsInitialilizer(
				app.Model.Postprocessing.PostprocessorsManager, 
				app.Model.UserDefinedFormatsManager, 
				new Chromium.StateInspector.PostprocessorsFactory()
			);
		}




		public override void Dispose()
		{
		}
	}
}
