using LogJoint.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		public Plugin()
		{
		}

		public override void Init(IApplication app)
		{
			Azure.Factory.RegisterFactories(app.Model.LogProviderFactoryRegistry, app.Model.TempFilesManager);
			app.Presentation.NewLogSourceDialog.PagesRegistry.RegisterPagePresenterFactory(
				Azure.Factory.uiTypeKey, 
				factory => new UI.Azure.FactoryUI((Azure.Factory)factory, app.Model.SourcesManager, app.Model.MRU)
			);
		}
	}
}
