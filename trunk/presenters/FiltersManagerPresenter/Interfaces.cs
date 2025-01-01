using System;

namespace LogJoint.UI.Presenters.FiltersManager
{
    public interface IPresenter
    {
        IFiltersList FiltersList { get; set; }
    };

    [Flags]
    public enum ViewControl
    {
        None = 0,
        AddFilterButton = 1,
        RemoveFilterButton = 2,
        MoveUpButton = 4,
        MoveDownButton = 8,
        PrevButton = 16,
        NextButton = 32,
        FilteringEnabledCheckbox = 64,
        FilterOptions = 128,
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        FiltersListBox.IViewModel FiltersListBox { get; }
        FilterDialog.IViewModel FilterDialog { get; }
        ViewControl VisibileControls { get; }
        ViewControl EnabledControls { get; }
        (bool isChecked, string tooltip, string label) FiltertingEnabledCheckBox { get; }
        void OnEnableFilteringChecked(bool value);
        void OnAddFilterClicked();
        void OnRemoveFilterClicked();
        void OnMoveFilterUpClicked();
        void OnMoveFilterDownClicked();
        void OnPrevClicked();
        void OnNextClicked();
        void OnOptionsClicked();
    };
};