﻿using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint.Analytics
{
	public interface IVisitable<Visitor>
	{
		void Visit(Visitor visitor);
 	};
}
