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
			LogJoint.Azure.Factory.Instance.GetHashCode();
		}
	}
}
