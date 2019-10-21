using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Correlation
{
	class SameNodeDetectionTokenFactories : ISameNodeDetectionTokenFactories
	{
		const string sameNodeDetectionTokenEltName = "sameNodeDetectionToken";
		const string factoryAttributeName = "_factory";
		readonly Dictionary<string, ISameNodeDetectionTokenFactory> factories = new Dictionary<string, ISameNodeDetectionTokenFactory>();

		public SameNodeDetectionTokenFactories()
		{
			((ISameNodeDetectionTokenFactories)this).Register(new NullSameNodeDetectionToken());
		}

		void ISameNodeDetectionTokenFactories.Register(ISameNodeDetectionTokenFactory factory) => factories[factory.Id] = factory;

		bool ISameNodeDetectionTokenFactories.TryReadLogPartToken(XElement element, out ISameNodeDetectionToken token)
		{
			token = null;
			if (element != null && element.Name.LocalName == sameNodeDetectionTokenEltName)
			{
				var factoryId = element.AttributeValue(factoryAttributeName);
				if (factories.TryGetValue(factoryId, out var tokenFactory))
					token = tokenFactory.Deserialize(element);
			}
			return token != null;
		}

		void ISameNodeDetectionTokenFactories.SafeWriteTo(ISameNodeDetectionToken token, XmlWriter writer)
		{
			if (token == null)
				return;
			var tokenElt = new XElement(sameNodeDetectionTokenEltName);
			token.Serialize(tokenElt);
			tokenElt.SetAttributeValue(factoryAttributeName, token.Factory.Id);
			tokenElt.WriteTo(writer);
		}
	};
}
