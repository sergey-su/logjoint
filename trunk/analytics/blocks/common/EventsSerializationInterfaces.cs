
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Analytics
{
	public interface IEventsSerializer
	{
		ICollection<XElement> Output { get; }
	};
}
