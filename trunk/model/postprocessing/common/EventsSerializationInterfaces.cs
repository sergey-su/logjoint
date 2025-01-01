
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
    public interface IEventsSerializer
    {
        ICollection<XElement> Output { get; }
    };
}
