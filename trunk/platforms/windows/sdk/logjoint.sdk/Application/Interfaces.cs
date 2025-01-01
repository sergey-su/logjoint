namespace LogJoint
{
    public interface IApplication
    {
        IModel Model { get; }
        UI.Presenters.IPresentation Presentation { get; }
        UI.Windows.IView View { get; }
    };
}
