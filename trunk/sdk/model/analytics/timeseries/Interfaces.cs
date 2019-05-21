using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace LogJoint.Analytics.TimeSeries
{
	/// <summary>
	/// Encapsulates loading and updating of time-series metadata
	/// </summary>
	public interface ITimeSeriesTypesAccess
	{
		void RegisterTimeSeriesTypesAssembly(Assembly asm);
		void CheckForCustomConfigUpdate();
		IEnumerable<Type> GetMetadataTypes();
		XmlSerializer GetEventsSerializer();
		XmlSerializer GetSeriesSerializer();
		string UserDefinedParserConfigPath { get; }
		string CustomConfigLoadingError { get; }
		string CustomConfigEnvVar { get; set; }
	};
}
