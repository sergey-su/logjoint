using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
    public class UnprocessedLogsToastNotification : IToastNotificationItem
    {
        readonly IManagerInternal ppm;
        readonly PostprocessorKind postprocessorKind;
        int nrOfUnprocessed;
        int nrOfInProgress;
        double? progress;

        public UnprocessedLogsToastNotification(
            IManagerInternal ppm,
            PostprocessorKind postprocessorKind
        )
        {
            this.ppm = ppm;
            this.postprocessorKind = postprocessorKind;
            ppm.Changed += (s, e) => Update();
            Update();
        }

        public event EventHandler<ItemChangeEventArgs> Changed;

        void IToastNotificationItem.PerformAction(string actionId)
        {
            ppm.RunPostprocessors(
                ppm.LogSourcePostprocessors.GetPostprocessorOutputsByPostprocessorId(postprocessorKind)
            );
        }

        bool IToastNotificationItem.IsActive
        {
            get { return nrOfUnprocessed > 0 || nrOfInProgress > 0; }
        }

        string IToastNotificationItem.Contents
        {
            get
            {
                if (nrOfInProgress > 0)
                    return "the view will be updated soon with new data";

                return string.Format("the view may be incomplete: data from {0} log(s) is missing.  *1 fix*",
                    nrOfUnprocessed);
            }
        }

        double? IToastNotificationItem.Progress
        {
            get { return progress; }
        }

        void Update()
        {
            var outputs = ppm.LogSourcePostprocessors.GetPostprocessorOutputsByPostprocessorId(postprocessorKind);

            int oldNrOfUnprocessed = nrOfUnprocessed;

            progress = null;
            nrOfUnprocessed = 0;
            nrOfInProgress = 0;
            foreach (var output in outputs)
            {
                switch (output.OutputStatus)
                {
                    case LogSourcePostprocessorState.Status.InProgress:
                    case LogSourcePostprocessorState.Status.Loading:
                        progress = output.Progress;
                        ++nrOfInProgress;
                        break;
                    case LogSourcePostprocessorState.Status.NeverRun:
                        ++nrOfUnprocessed;
                        break;
                }
            }

            Changed?.Invoke(this, new ItemChangeEventArgs(isUnsuppressingChange: nrOfUnprocessed > oldNrOfUnprocessed));
        }
    };
}
