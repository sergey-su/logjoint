using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public class LogProviderUIsRegistry : ILogProviderUIsRegistry
	{
		Dictionary<string, Func<ILogProviderFactory, ILogProviderUI>> factories = new Dictionary<string, Func<ILogProviderFactory, ILogProviderUI>>();

		ILogProviderUI ILogProviderUIsRegistry.CreateProviderUI(ILogProviderFactory factory)
		{
			Func<ILogProviderFactory, ILogProviderUI> uiFactoryMethod;
			if (!factories.TryGetValue(factory.UITypeKey, out uiFactoryMethod))
				return null;
			return uiFactoryMethod(factory);
		}

		void ILogProviderUIsRegistry.Register(string key, Func<ILogProviderFactory, ILogProviderUI> createUi)
		{
			factories[key] = createUi;
		}
	}
}
