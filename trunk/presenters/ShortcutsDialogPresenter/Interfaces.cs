
namespace LogJoint.UI.Presenters.ShortcutsDialog
{
    public interface IPresenter
    {
        void ShowDialog();
    }

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        bool IsVisible { get; }
        void OnCloseRequested();
    }
}
