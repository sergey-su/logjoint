using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TSBlocks = LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Postprocessing.TimeSeries
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

	public interface ITimeSeriesPostprocessorOutput
	{
		ILogSource LogSource { get; }

		/// <summary>
		/// Log display name as a separate property because LogSource may be disposed while
		/// Display name is still required.
		/// </summary>
		string LogDisplayName { get; }

		IEnumerable<TSBlocks.TimeSeriesData> TimeSeries { get; }

		IEnumerable<TSBlocks.EventBase> Events { get; }
	}

	public interface ITimeSeriesVisualizerModel
	{
		ICollection<ITimeSeriesPostprocessorOutput> Outputs { get; }

		event EventHandler Changed;
	};

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
