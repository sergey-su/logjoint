using System;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchEditorDialog
{
    public interface IPresenter
    {
        Task<bool> Open(IUserDefinedSearch search);
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        bool IsVisible { get; }
        string Name { get; }
        FiltersManager.IViewModel FiltersManager { get; }
        void OnConfirmed();
        void OnCancelled();
        void OnChangeName(string name);
    };
};