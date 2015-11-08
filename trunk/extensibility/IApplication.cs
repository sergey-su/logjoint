namespace LogJoint.Extensibility
{
	public interface IApplication
	{
		Extensibility.IModel Model { get; }
		Extensibility.IPresentation Presentation { get; }
		Extensibility.IView View { get; }
	};
}
