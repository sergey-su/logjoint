using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
    interface ILogPartTokenFactories
    {
        void Register(ILogPartTokenFactory factory);
        bool TryReadLogPartToken(XElement element, out ILogPartToken token);
        void SafeWriteTo(ILogPartToken logPartToken, XmlWriter writer);
    }
}
