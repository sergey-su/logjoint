using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public interface IThread : IDisposable
	{
		bool IsDisposed { get; }
		string ID { get; }
		string Description { get; }
		string DisplayName { get; }
		bool Visible { get; set; }
		bool ThreadMessagesAreVisible { get; }
		ModelColor ThreadColor { get; }
		IBookmark FirstKnownMessage { get; }
		IBookmark LastKnownMessage { get; }
		ILogSource LogSource { get; }
	}

	public struct ThreadsBulkProcessingResult
	{
		public FilterContext HighlightFilterContext { get { return info.highlightFilterContext; } }
		public bool ThreadWasInCollapsedRegion { get { return threadWasInCollapsedRegion; } }
		public bool ThreadIsInCollapsedRegion { get { return threadIsInCollapsedRegion; } }

		internal ModelThreads.ThreadsBulkProcessing.ThreadInfo info;
		internal bool threadWasInCollapsedRegion;
		internal bool threadIsInCollapsedRegion;
	};

	public interface IThreadsBulkProcessing : IDisposable
	{
		ThreadsBulkProcessingResult ProcessMessage(IMessage message);
		void HandleHangingFrames(IMessagesCollection messagesCollection);
	};

	public interface IModelThreads
	{
		event EventHandler OnThreadListChanged;
		event EventHandler OnThreadVisibilityChanged;
		event EventHandler OnPropertiesChanged;
		IEnumerable<IThread> Items { get; }
		IThreadsBulkProcessing StartBulkProcessing();
		IColorTable ColorTable { get; }

		IThread RegisterThread(string id, ILogSource logSource);
	};

	public interface ILogSourceThreads : IDisposable
	{
		IModelThreads UnderlyingThreadsContainer { get; }
		IEnumerable<IThread> Items { get; }
		IThread GetThread(StringSlice id);
		void DisposeThreads();
	};

	public class FilterContext
	{
		public void Reset()
		{
			filterRegionDepth = 0;
			regionFilter = null;
		}

		public void BeginRegion(IFilter filter)
		{
			if (filterRegionDepth == 0)
				regionFilter = filter;
			else
				System.Diagnostics.Debug.Assert(filter == regionFilter);
			++filterRegionDepth;
		}

		public void EndRegion()
		{
			--filterRegionDepth;
			if (filterRegionDepth == 0)
				regionFilter = null;
		}

		public IFilter RegionFilter
		{
			get { return regionFilter; }
		}

		int filterRegionDepth;
		IFilter regionFilter;
	};
}
