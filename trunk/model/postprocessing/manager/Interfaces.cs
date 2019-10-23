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
		IReadOnlyList<ILogSource> KnownLogSources { get; } // todo: could be removed?
		IReadOnlyList<LogSourceMetadata> KnownLogTypes { get; } // todo: could be removed?

	};
}
