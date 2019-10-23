using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using LogJoint.UI.Presenters.ToastNotificationPresenter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
	public class CorrelatorToastNotification: IToastNotificationItem
	{
		IManagerInternal ppm;
		CorrelationStateSummary lastSummary;

		public CorrelatorToastNotification(
			IManagerInternal ppm,
			ILogSourcesManager lsm
		)
		{
			this.ppm = ppm;
			ppm.Changed += (s, e) => Update();
			lsm.OnLogSourceTimeOffsetChanged += (s, e) => Update();
			Update();
		}

		public event EventHandler<ItemChangeEventArgs> Changed;

		async void IToastNotificationItem.PerformAction (string actionId)
		{
			switch (lastSummary.Status)
			{
			case CorrelationStateSummary.StatusCode.NeedsProcessing:
			case CorrelationStateSummary.StatusCode.Processed:
			case CorrelationStateSummary.StatusCode.ProcessingFailed:

				await this.ppm.RunPostprocessor(
					ppm.GetPostprocessorOutputsByPostprocessorId(PostprocessorKind.Correlator)
						.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.Postprocessor, output.LogSource))
						.ToArray()
				);
				break;
			}
		}

		bool IToastNotificationItem.IsActive
		{
			get { return IsActiveImpl (); }
		}

		string IToastNotificationItem.Contents
		{
			get
			{
				switch (lastSummary.Status)
				{
				case CorrelationStateSummary.StatusCode.NeedsProcessing:
					return "view may be inaccurate: clocks sync required.  *1 fix*";
				case CorrelationStateSummary.StatusCode.ProcessingInProgress:
					return "clocks skew is being fixed";
				case CorrelationStateSummary.StatusCode.ProcessingFailed:
					return "view may be inaccurate: clocks cannot be synched";
				}
				return "";
			}
		}

		double? IToastNotificationItem.Progress
		{
			get { return lastSummary.Progress; }
		}

		void Update()
		{
			bool wasActive = IsActiveImpl ();
			// lastSummary = ppm.GetCorrelatorStateSummary(); todo
			if (Changed != null)
			{
				Changed(this, new ItemChangeEventArgs(isUnsuppressingChange: IsActiveImpl() && !wasActive));
			}
		}

		bool IsActiveImpl ()
		{
			if (lastSummary.Status == CorrelationStateSummary.StatusCode.PostprocessingUnavailable 
			|| lastSummary.Status == CorrelationStateSummary.StatusCode.Processed)
			{
				return false;
			}
			return true;
		}
	};
}
