using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
	public interface IOutputDataDeserializer
	{
		object Deserialize(PostprocessorKind kind, LogSourcePostprocessorDeserializationParams p);
	};

	public interface IManagerInternal: IManager
	{
		event EventHandler Changed; // todo: remove
	};

	public interface ICorrelationManager
	{
		Correlation.CorrelatorStateSummary StateSummary { get; }
		void Run();
	};
}
