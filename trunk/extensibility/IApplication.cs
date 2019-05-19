namespace LogJoint.Extensibility
{
	public interface IApplication // todo: remove
	{
		Extensibility.IModel Model { get; }
		Extensibility.IPresentation Presentation { get; }
		Extensibility.IView View { get; }
	};
}
