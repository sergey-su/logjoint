using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogJoint.Postprocessing.TimeSeries
{

	#region Internal parsing interfaces

	public interface ILineParser
	{
		string GetPrefix();
		UInt32 GetNumericId();

		void Parse(string text, ILineParserVisitor visitor, string objectAddress);

		Type GetMetadataSource();
	}

	public interface ILineParserVisitor
	{
		void VisitTimeSeries(TimeSeriesDescriptor descriptor, string objectId,
			string dynamicName, string dynamicUnit, double value);

		void VisitEvent(EventBase e);
	}

	#endregion

}
