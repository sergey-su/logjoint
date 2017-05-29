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
		IPostprocessorsManager ppm;
		string postprocessorId;
		int nrOfUnprocessed;
		double? progress;

		public UnprocessedLogsToastNotification(
			IPostprocessorsManager ppm,
			ILogSourcesManager lsm,
			string postprocessorId
		)
		{
			this.ppm = ppm;
			this.postprocessorId = postprocessorId;
			ppm.Changed += (s, e) => Update();
			Update();
		}

		public event EventHandler<ItemChangeEventArgs> Changed;

		void IToastNotificationItem.PerformAction (string actionId)
		{
			ppm.RunPostprocessor(
				ppm.GetPostprocessorOutputsByPostprocessorId(postprocessorId)
				.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.PostprocessorMetadata, output.LogSource))
				.ToArray(),
				forceSourcesSelection: false
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
			var outputs = ppm.GetPostprocessorOutputsByPostprocessorId(postprocessorId);

			int oldNrOfUnprocessed = nrOfUnprocessed;

			progress = null;
			nrOfUnprocessed = 0;
			foreach (var output in outputs)
			{
				switch (output.OutputStatus)
				{
				case LogSourcePostprocessorOutput.Status.InProgress:
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
