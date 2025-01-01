using System;
using System.Collections.Generic;

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

    public interface IView
    {
        void SetEventsHandler(IViewEvents handler);
        void SetLabel(string value);
        void EnableControls(bool enableUp, bool enableDown, bool enableLabel);
    };

    public interface IViewEvents
    {
        void OnUpButtonClicked();
        void OnDownButtonClicked();
    };
};