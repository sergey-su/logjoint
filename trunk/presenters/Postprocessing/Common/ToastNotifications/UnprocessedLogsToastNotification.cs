using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
	public class UnprocessedLogsToastNotification: IToastNotificationItem
	{
		IManagerInternal ppm;
		PostprocessorKind postprocessorKind;
		int nrOfUnprocessed;
		double? progress;

		public UnprocessedLogsToastNotification(
			IManagerInternal ppm,
			ILogSourcesManager lsm,
			PostprocessorKind postprocessorKind
		)
		{
			this.ppm = ppm;
			this.postprocessorKind = postprocessorKind;
			ppm.Changed += (s, e) => Update();
			Update();
		}

		public event EventHandler<ItemChangeEventArgs> Changed;

		void IToastNotificationItem.PerformAction (string actionId)
		{
			ppm.RunPostprocessors(
				ppm.LogSourcePostprocessors.GetPostprocessorOutputsByPostprocessorId(postprocessorKind)
			);
		}

		bool IToastNotificationItem.IsActive
		{
			get { return nrOfUnprocessed > 0 || progress != null; }
		}

		string IToastNotificationItem.Contents
		{
			get
			{
				if (progress != null)
					return "view will be updated soon with new data";

				return string.Format("view may be incomplete: data from {0} logs is missing.  *1 fix*",
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
			foreach (var output in outputs)
			{
				switch (output.OutputStatus)
				{
				case LogSourcePostprocessorState.Status.InProgress:
				case LogSourcePostprocessorState.Status.Loading:
					progress = output.Progress;
					break;
				case LogSourcePostprocessorState.Status.NeverRun:
					++nrOfUnprocessed;
					break;
				}
			}

			if (Changed != null)
				Changed(this, new ItemChangeEventArgs(isUnsuppressingChange: nrOfUnprocessed > oldNrOfUnprocessed));
		}
	};
}
