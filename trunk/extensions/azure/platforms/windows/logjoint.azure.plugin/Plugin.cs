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

		public override void Init(ILogJointApplication app)
		{
			LogJoint.Azure.Factory.RegisterInstances(app.Model.LogProviderFactoryRegistry);
			app.LogProviderUIsRegistry.Register(LogJoint.Azure.Factory.uiTypeKey, factory => new LogJoint.UI.Azure.FactoryUI((Azure.Factory)factory, app.Model.MRU));
		}
	}
}
