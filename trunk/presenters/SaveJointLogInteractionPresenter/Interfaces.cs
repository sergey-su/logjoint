using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.SaveJointLogInteractionPresenter
{
    public interface IPresenter
    {
        void StartInteraction();
        bool IsInteractionInProgress { get; }
    };
};