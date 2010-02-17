using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public class UpdateTracker
	{
		public void InvalidateMessages()
		{
			messagesUpdateFlag = 1;
		}
		public void InvalidateTimeLine()
		{
			timeLineUpdateFlag = 1;
		}
		public void InvalidateThreads()
		{
			threadsUpdateFlag = 1;
		}
		public void InvalidateSources()
		{
			sourcesUpdateFlag = 1;
		}
		public void InvalidateFilters()
		{
			filtersUpdateFlag = 1;
		}
		public void InvalidateHighlightFilters()
		{
			highlightFiltersUpdateFlag = 1;
		}
		public void InvalidateTimeGaps()
		{
			timeGapsUpdateFlag = 1;
		}

		public bool ValidateMessages()
		{
			return Interlocked.CompareExchange(ref messagesUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateTimeline()
		{
			return Interlocked.CompareExchange(ref timeLineUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateThreads()
		{
			return Interlocked.CompareExchange(ref threadsUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateSources()
		{
			return Interlocked.CompareExchange(ref sourcesUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateFilters()
		{
			return Interlocked.CompareExchange(ref filtersUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateHighlightFilters()
		{
			return Interlocked.CompareExchange(ref highlightFiltersUpdateFlag, 0, 1) != 0;
		}

		public bool ValidateTimeGaps()
		{
			return Interlocked.CompareExchange(ref timeGapsUpdateFlag, 0, 1) != 0;
		}

		int threadsUpdateFlag;
		int messagesUpdateFlag;
		int timeLineUpdateFlag;
		int sourcesUpdateFlag;
		int filtersUpdateFlag;
		int highlightFiltersUpdateFlag;
		int timeGapsUpdateFlag;
	};
}
