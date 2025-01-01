using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
    class LogPartTokenFactories : ILogPartTokenFactories
    {
        const string rotatedLogPartTokenEltName = "rotatedLogPartToken";
        const string factoryAttributeName = "_factory";
        readonly Dictionary<string, ILogPartTokenFactory> factories = new Dictionary<string, ILogPartTokenFactory>();

        public LogPartTokenFactories()
        {
            ((ILogPartTokenFactories)this).Register(new NullLogPartToken());
        }

        void ILogPartTokenFactories.Register(ILogPartTokenFactory factory) => factories[factory.Id] = factory;

        bool ILogPartTokenFactories.TryReadLogPartToken(XElement element, out ILogPartToken token)
        {
            token = null;
            if (element != null && element.Name.LocalName == rotatedLogPartTokenEltName)
            {
                var factoryId = element.AttributeValue(factoryAttributeName);
                if (factories.TryGetValue(factoryId, out var tokenFactory))
                    token = tokenFactory.Deserialize(element);
            }
            return token != null;
        }

        void ILogPartTokenFactories.SafeWriteTo(ILogPartToken logPartToken, XmlWriter writer)
        {
            if (logPartToken == null)
                return;
            var tokenElt = new XElement(rotatedLogPartTokenEltName);
            logPartToken.Serialize(tokenElt);
            tokenElt.SetAttributeValue(factoryAttributeName, logPartToken.Factory.Id);
            tokenElt.WriteTo(writer);
        }
    };
}
