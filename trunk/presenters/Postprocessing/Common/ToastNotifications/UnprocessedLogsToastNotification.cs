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
		IManager ppm;
		PostprocessorKind postprocessorKind;
		int nrOfUnprocessed;
		double? progress;

		public UnprocessedLogsToastNotification(
			IManager ppm,
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
			ppm.RunPostprocessor(
				ppm.GetPostprocessorOutputsByPostprocessorId(postprocessorKind)
				.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.Postprocessor, output.LogSource))
				.ToArray()
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
			var outputs = ppm.GetPostprocessorOutputsByPostprocessorId(postprocessorKind);

			int oldNrOfUnprocessed = nrOfUnprocessed;

			progress = null;
			nrOfUnprocessed = 0;
			foreach (var output in outputs)
			{
				switch (output.OutputStatus)
				{
				case LogSourcePostprocessorOutput.Status.InProgress:
				case LogSourcePostprocessorOutput.Status.Loading:
					progress = output.Progress;
					break;
				case LogSourcePostprocessorOutput.Status.NeverRun:
					++nrOfUnprocessed;
					break;
				}
			}

			if (Changed != null)
				Changed(this, new ItemChangeEventArgs(isUnsuppressingChange: nrOfUnprocessed > oldNrOfUnprocessed));
		}
	};
}
