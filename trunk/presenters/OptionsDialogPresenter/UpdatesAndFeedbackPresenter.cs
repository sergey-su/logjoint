using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Settings;
using LogJoint.AutoUpdate;

namespace LogJoint.UI.Presenters.Options.UpdatesAndFeedback
{
    public class Presenter : IPresenter, IViewModel, IPresenterInternal
    {
        public Presenter(
            IAutoUpdater model,
            IGlobalSettingsAccessor settingsAccessor)
        {
            this.model = model;
        }

        void IViewModel.SetView(IView view)
        {
            this.view = view;

            UpdateAutomaticUpdatesView();
            model.Changed += (s, e) => UpdateAutomaticUpdatesView();
        }

        bool IPresenter.Apply()
        {
            return true;
        }

        bool IPresenter.IsAvailable
        {
            get { return model.State != AutoUpdateState.Disabled; }
        }

        void IViewModel.OnCheckUpdateNowClicked()
        {
            model.CheckNow();
        }

        #region Implementation

        void UpdateAutomaticUpdatesView()
        {
            var pres = model.GetPresentation(preferShortBrief: false);
            view.SetLastUpdateCheckInfo(pres.Brief, pres.Details);
            view.SetCheckNowButtonAvailability(pres.CanCheckNow);
        }

        readonly IAutoUpdater model;
        IView view;

        #endregion
    };
};