using System;

namespace LogJoint.UI.Presenters.LabeledStepperPresenter
{
    public interface IPresenter
    {
        event EventHandler<EventArgs> OnValueChanged;
        int[] AllowedValues { get; set; }
        int MinValue { get; set; }
        int MaxValue { get; set; }
        int Value { get; set; }
        bool Enabled { get; set; }
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        string Label { get; }
        bool EnabledUp { get; }
        bool EnabledDown { get; }
        bool EnabledLabel { get; }
        void OnUpButtonClicked();
        void OnDownButtonClicked();
    };

    public interface IPresenterInternal : IPresenter, IViewModel { };
};