namespace LogJoint.Postprocessing
{
	public interface IOutputDataDeserializer
	{
		object Deserialize(PostprocessorKind kind, LogSourcePostprocessorDeserializationParams p);
	};
}
