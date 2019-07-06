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
		IManager ppm;
		CorrelatorStateSummary lastSummary;

		public CorrelatorToastNotification(
			IManager ppm,
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
			case CorrelatorStateSummary.StatusCode.NeedsProcessing:
			case CorrelatorStateSummary.StatusCode.Processed:
			case CorrelatorStateSummary.StatusCode.ProcessingFailed:

				await this.ppm.RunPostprocessor(
					ppm.GetPostprocessorOutputsByPostprocessorId(PostprocessorKind.Correlator)
						.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.PostprocessorMetadata, output.LogSource))
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
				case CorrelatorStateSummary.StatusCode.NeedsProcessing:
					return "view may be inaccurate: clocks sync required.  *1 fix*";
				case CorrelatorStateSummary.StatusCode.ProcessingInProgress:
					return "clocks skew is being fixed";
				case CorrelatorStateSummary.StatusCode.ProcessingFailed:
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
			lastSummary = ppm.GetCorrelatorStateSummary();
			if (Changed != null)
			{
				Changed(this, new ItemChangeEventArgs(isUnsuppressingChange: IsActiveImpl() && !wasActive));
			}
		}

		bool IsActiveImpl ()
		{
			if (lastSummary.Status == CorrelatorStateSummary.StatusCode.PostprocessingUnavailable 
			|| lastSummary.Status == CorrelatorStateSummary.StatusCode.Processed)
			{
				return false;
			}
			return true;
		}
	};
}
