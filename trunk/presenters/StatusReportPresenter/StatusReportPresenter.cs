using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.StatusReports
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly List<StatusPopup> shownPopups = new List<StatusPopup>();
        readonly List<StatusPopup> shownTexts = new List<StatusPopup>();
        readonly IChangeNotification changeNotification;
        string statusText;
        PopupData popup;
        bool cancelLongRunningControlVisibile;
        Task timer;

        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        internal void SetStatusText(string value)
        {
            statusText = value;
            changeNotification.Post();
        }

        internal void SetPopupData(PopupData data)
        {
            popup = data;
            changeNotification.Post();
        }

        internal void SetCancelLongRunningControlsVisibility(bool value)
        {
            cancelLongRunningControlVisibile = value;
            changeNotification.Post();
        }

        IReport IPresenter.CreateNewStatusReport()
        {
            return new StatusPopup(this);
        }

        void IPresenter.CancelActiveStatus()
        {
            CancelActiveStatusInternal();
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;
        string IViewModel.StatusText => statusText;
        bool IViewModel.CancelLongRunningControlVisibile => cancelLongRunningControlVisibile;
        PopupData IViewModel.PopupData => popup;

        void IViewModel.OnCancelLongRunningProcessButtonClicked()
        {
            CancelActiveStatusInternal();
        }

        void CancelActiveStatusInternal()
        {
            var rpt = GetActiveReport(shownTexts);
            if (rpt != null)
                rpt.Cancel();
        }

        static StatusPopup GetActiveReport(List<StatusPopup> shownReports)
        {
            return shownReports.LastOrDefault();
        }

        async Task Timeslice()
        {
            while (shownPopups.Count > 0)
            {
                await Task.Delay(100);
                foreach (var r in shownPopups.ToArray())
                    r.AutoHideIfItIsTime();
            }
            timer = null;
        }

        internal void ReportsTransaction(Action<List<StatusPopup>> body, bool allowReactivation, bool isPopup)
        {
            var list = isPopup ? shownPopups : shownTexts;
            var oldActive = GetActiveReport(list);
            body(list);
            var newActive = GetActiveReport(list);
            if (newActive == oldActive && !allowReactivation)
                return;
            if (oldActive != null)
                oldActive.Deactivate();
            if (newActive != null)
                newActive.Activate();
            if (shownPopups.Count > 0 && timer == null)
                timer = Timeslice();
        }
    }
};