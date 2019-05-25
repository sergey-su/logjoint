using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	public interface IVisitable<Visitor>
	{
		void Visit(Visitor visitor);
 	};
}
