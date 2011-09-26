using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class ConnectionParams : SemicolonSeparatedMap, IConnectionParams
	{
		public ConnectionParams(string str): base(str)
		{
		}
		public ConnectionParams(): this("")
		{
		}
		public void AssignFrom(IConnectionParams other)
		{
			base.AssignFrom((SemicolonSeparatedMap)other);
		}
		public bool AreEqual(IConnectionParams other)
		{
			SemicolonSeparatedMap map = other as SemicolonSeparatedMap;
			if (map == null)
				return false;
			return base.AreEqual(map);
		}
		public IConnectionParams Clone()
		{
			var ret = new ConnectionParams();
			ret.AssignFrom(this);
			return ret;
		}
	}
}
