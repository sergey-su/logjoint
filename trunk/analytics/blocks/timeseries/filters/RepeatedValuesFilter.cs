using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Analytics.TimeSeries.Filters
{
	public static class RemoveRepeatedValues
	{
		public static IEnumerable<DataPoint> Filter(IEnumerable<DataPoint> dataPoints)	
		{
			return dataPoints.FilterOutRepeatedKeys(
				(p1, p2) => Math.Abs(p1.Value - p2.Value) < 1e-5d, numericSemantics: true);
		}
	}
}
