using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class ConnectionParams :SemicolonSeparatedMap, IConnectionParams
	{
		public ConnectionParams(string str): base(str)
		{
		}
		public ConnectionParams(): this("")
		{
		}
		public void Assign(IConnectionParams other)
		{
			base.Assign((SemicolonSeparatedMap)other);
		}
		public bool AreEqual(IConnectionParams other)
		{
			SemicolonSeparatedMap map = other as SemicolonSeparatedMap;
			if (map == null)
				return false;
			return base.AreEqual(map);
		}
	}
}
