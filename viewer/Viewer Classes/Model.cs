using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace LogJoint
{
	public interface IModelHost
	{
		Source Tracer { get; }
		ISynchronizeInvoke Invoker { get; }
		ITempFilesManager TempFilesManager { get; }

		void OnNewReader(ILogReader reader);
		IStatusReport GetStatusReport();

		DateTime? CurrentViewTime { get; }
		void SetCurrentViewTime(DateTime? time, NavigateFlag flags);
		
		MessageBase FocusedMessage { get; }

		void OnUpdateView();

		bool FocusRectIsRequired { get; }
		IUINavigationHandler UINavigationHandler { get; }

		IMainForm MainFormObject { get; }
	};

	public class FiltersListViewHost : UI.IFiltersListViewHost
	{
		public FiltersListViewHost(FiltersList filtersList, bool isHLFilter, LogSourcesManager logSources)
		{
			this.logSources = logSources;
			this.filtersList = filtersList;
			this.isHLFilter = isHLFilter;
		}

		public FiltersList Filters { get { return filtersList; } }
		public IEnumerable<ILogSource> LogSources { get { return logSources.Items; } }
		public bool IsHighlightFilter { get { return isHLFilter; } }


		readonly LogSourcesManager logSources;
		readonly FiltersList filtersList;
		readonly bool isHLFilter;
	};

	public class Model: 
		IDisposable,
		IFactoryUICallback,
		ILogSourcesManagerHost,
		ITimeGapsHost,
		UI.ILogViewerControlHost,
		UI.ITimeLineControlHost,
		UI.IThreadsListViewHost,
		UI.ISourcesListViewHost
	{
		readonly Source tracer;
		readonly IModelHost host;
		readonly UpdateTracker updates;
		readonly LogSourcesManager logSources;
		readonly Threads threads;
		readonly Bookmarks bookmarks;
		readonly SourcesCollection sourcesCollection;
		readonly FiltersList displayFilters;
		readonly FiltersListViewHost displayFiltersListViewHost;
		readonly FiltersList highlightFilters;
		readonly FiltersListViewHost highlightFiltersListViewHost;
		readonly TimeGaps timeGaps;
		readonly ColorTableBase filtersColorTable;


		public Model(IModelHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;
			updates = new UpdateTracker();
			threads = new Threads();
			threads.OnThreadListChanged += threads_OnThreadListChanged;
			threads.OnThreadVisibilityChanged += threads_OnThreadVisibilityChanged;
			threads.OnPropertiesChanged += threads_OnPropertiesChanged;
			logSources = new LogSourcesManager(this);
			sourcesCollection = new SourcesCollection(logSources.Items);
			bookmarks = new Bookmarks();
			bookmarks.OnBookmarksChanged += new EventHandler(bookmarks_OnBookmarksChanged);
			displayFilters = new FiltersList(FilterAction.Include);
			displayFilters.OnFiltersListChanged += new EventHandler(filters_OnFiltersListChanged);
			displayFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(filters_OnPropertiesChanged);
			displayFiltersListViewHost = new FiltersListViewHost(displayFilters, false, logSources);
			highlightFilters = new FiltersList(FilterAction.Exclude);
			highlightFilters.OnFiltersListChanged += new EventHandler(highlightFilters_OnFiltersListChanged);
			highlightFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(highlightFilters_OnPropertiesChanged);
			highlightFiltersListViewHost = new FiltersListViewHost(highlightFilters, true, logSources);
			timeGaps = new TimeGaps(this);
			timeGaps.OnTimeGapsChanged += new EventHandler(timeGaps_OnTimeGapsChanged);
			filtersColorTable = new HTMLColorsGenerator();
		}

		void highlightFilters_OnPropertiesChanged(object sender, FilterChangeEventArgs e)
		{
			updates.InvalidateHighlightFilters();
			if (e.ChangeAffectsFilterResult)
				updates.InvalidateMessages();
		}

		void highlightFilters_OnFiltersListChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
			updates.InvalidateMessages();
		}

		public void Dispose()
		{
			DeleteLogs();
			timeGaps.Dispose();
			displayFilters.Dispose();
			highlightFilters.Dispose();
		}

		public Source Tracer { get { return tracer; } }

		public UpdateTracker Updates { get { return updates; } }

		public Bookmarks Bookmarks { get { return bookmarks; } }

		public TimeGaps TimeGaps { get { return timeGaps; } }

		public FiltersListViewHost DisplayFiltersListViewHost { get { return displayFiltersListViewHost; } }

		public FiltersListViewHost HighlightFiltersListViewHost { get { return highlightFiltersListViewHost; } }

		public void DeleteLogs(ILogSource[] logs)
		{
			foreach (ILogSource s in logs)
				s.Dispose();
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			updates.InvalidateMessages();
			updates.InvalidateTimeLine();
			timeGaps.Invalidate();
		}

		public void DeleteLogs()
		{
			DeleteLogs(new List<ILogSource>(logSources.Items).ToArray());
		}

		public ILogReader LoadFrom(FormatAutodetect.DetectedFormat fmtInfo)
		{
			return LoadFrom(fmtInfo.Factory, fmtInfo.ConnectParams);
		}

		public ILogReader LoadFrom(RecentLogEntry entry)
		{
			return LoadFrom(entry.Factory, entry.ConnectionParams);
		}

		ILogReader LoadFrom(ILogReaderFactory factory, IConnectionParams cp)
		{
			ILogSource src = null;
			ILogReader reader = null;
			try
			{
				reader = FindExistingReader(cp);
				if (reader != null)
					return reader;
				src = logSources.Create();
				reader = factory.CreateFromConnectionParams(src, cp);
				src.Init(reader);
			}
			catch
			{
				if (reader != null)
					reader.Dispose();
				if (src != null)
					src.Dispose();
				throw;
			}
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			return reader;
		}

		public bool IsInViewTailMode 
		{
			get { return this.logSources.IsInViewTailMode; }
		}

		public void Refresh()
		{
			logSources.Refresh();
		}

		public void NavigateTo(DateTime time, NavigateFlag flag)
		{
			logSources.NavigateTo(time, flag);
		}

		public void SetCurrentViewPositionIfNeeded()
		{
			logSources.SetCurrentViewPositionIfNeeded();
		}

		public void OnCurrentViewPositionChanged(DateTime? d)
		{
			logSources.OnCurrentViewPositionChanged(d);
		}

		public void CancelShifting()
		{
			logSources.CancelShifting();
		}

		#region IFactoryUICallback Members

		public ILogReaderHost CreateHost()
		{
			return logSources.Create();
		}

		public void AddNewReader(ILogReader reader)
		{
			((ILogSource)reader.Host).Init(reader);
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			host.OnNewReader(reader);
			timeGaps.Invalidate();
		}

		public ILogReader FindExistingReader(IConnectionParams connectParams)
		{
			ILogSource s = logSources.Find(connectParams);
			if (s == null)
				return null;
			return s.Reader;
		}

		#endregion

		#region ILogViewerControlHost
		
		public Source Trace { get { return tracer; } }

		public IMessagesCollection Messages
		{
			get { return sourcesCollection; }
		}

		IEnumerable<IThread> UI.ILogViewerControlHost.Threads
		{
			get { return threads.Items; }
		}

		public void ShiftUp()
		{
			logSources.ShiftUp();
		}
		
		public bool IsShiftableUp
		{
			get { return logSources.IsShiftableUp; } 
		}

		public void ShiftDown()
		{
			logSources.ShiftDown();
		}

		public bool IsShiftableDown
		{
			get { return logSources.IsShiftableDown; }
		}

		public void ShiftAt(DateTime t)
		{
			logSources.ShiftAt(t);
		}

		IBookmarks UI.ILogViewerControlHost.Bookmarks
		{
			get { return bookmarks; }
		}

		IUINavigationHandler UI.ILogViewerControlHost.UINavigationHandler
		{
			get { return host.UINavigationHandler; }
		}

		public IMainForm MainForm
		{
			get { return host.MainFormObject; }
		}

		public FiltersList DisplayFilters 
		{
			get { return displayFilters; } 
		}

		public FiltersList HighlightFilters
		{
			get { return highlightFilters; }
		}

		#endregion

		#region ITimeLineControlHost Members

		public IEnumerable<UI.ITimeLineSource> Sources
		{
			get
			{
				foreach (ILogSource s in logSources.Items)
					if (s.Visible)
						yield return (UI.ITimeLineSource)s;
			}
		}

		public int SourcesCount
		{
			get
			{
				int ret = 0;
				foreach (ILogSource ls in logSources.Items)
					if (ls.Visible)
						++ret;
				return ret;
			}
		}

		public DateTime? CurrentViewTime
		{
			get { return host.CurrentViewTime; }
		}

		public IStatusReport GetStatusReport()
		{
			return host.GetStatusReport();
		}

		IEnumerable<IBookmark> UI.ITimeLineControlHost.Bookmarks
		{
			get
			{
				return bookmarks.Items;
			}
		}

		public bool FocusRectIsRequired 
		{
			get { return host.FocusRectIsRequired; }
		}

		ITimeGaps UI.ITimeLineControlHost.TimeGaps
		{
			get { return this.timeGaps.Gaps; }
		}

		#endregion

		#region ISourcesListViewHost Members

		IEnumerable<ILogSource> UI.ISourcesListViewHost.LogSources
		{
			get { return logSources.Items; }
		}

		IUINavigationHandler UI.ISourcesListViewHost.UINavigationHandler
		{
			get { return host.UINavigationHandler; }
		}

		ILogSource UI.ISourcesListViewHost.FocusedMessageSource
		{
			get 
			{
				MessageBase focused = host.FocusedMessage;
				if (focused == null)
					return null;
				return focused.Thread.LogSource; 
			}
		}

		#endregion

		#region IThreadsListViewHost Members

		IEnumerable<IThread> UI.IThreadsListViewHost.Threads
		{
			get { return threads.Items; }
		}

		IUINavigationHandler UI.IThreadsListViewHost.UINavigationHandler
		{
			get { return host.UINavigationHandler; }
		}

		IThread UI.IThreadsListViewHost.FocusedMessageThread 
		{
			get
			{
				MessageBase msg = host.FocusedMessage;
				if (msg == null)
					return null;
				return msg.Thread;
			}
		}

		#endregion

		#region ILogSourcesManagerHost Members

		public ISynchronizeInvoke Invoker
		{
			get { return host.Invoker; }
		}

		public ITempFilesManager TempFilesManager 
		{ 
			get { return host.TempFilesManager; }
		}
		
		public void SetCurrentViewPosition(DateTime? time, NavigateFlag flags)
		{
			host.SetCurrentViewTime(time, flags);
		}

		public void OnUpdateView()
		{
			host.OnUpdateView();
		}

		Threads ILogSourcesManagerHost.Threads { get { return threads; } }

		#endregion

		class SourcesCollection : MessagesContainers.MergeCollection
		{
			IEnumerable<ILogSource> list;

			public SourcesCollection(IEnumerable<ILogSource> list)
			{
				this.list = list;
			}

			protected override void Lock()
			{
				foreach (ILogSource ls in list)
					ls.Reader.LockMessages();
			}

			protected override void Unlock()
			{
				foreach (ILogSource ls in list)
					ls.Reader.UnlockMessages();
			}

			protected override IEnumerable<IMessagesCollection> GetCollectionsToMerge()
			{
				foreach (ILogSource ls in list)
					if (ls.Visible)
						yield return ls.Reader.Messages;
			}
		};

		#region ITimeGapsHost Members

		IEnumerable<ILogSource> ITimeGapsHost.Sources
		{
			get 
			{ 
				foreach (ILogSource ls in logSources.Items)
					if (ls.Visible && ls.Reader.Stats.State != ReaderState.LoadError)
						yield return ls; 
			}
		}

		#endregion

		void timeGaps_OnTimeGapsChanged(object sender, EventArgs e)
		{
			updates.InvalidateTimeLine();
		}

		void threads_OnThreadListChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
		}

		void threads_OnThreadVisibilityChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
			updates.InvalidateMessages();
		}

		void threads_OnPropertiesChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
		}

		void bookmarks_OnBookmarksChanged(object sender, EventArgs e)
		{
			updates.InvalidateTimeLine();
			updates.InvalidateMessages();
		}

		void filters_OnPropertiesChanged(object sender, FilterChangeEventArgs e)
		{
			updates.InvalidateFilters();
			if (e.ChangeAffectsFilterResult)
				updates.InvalidateMessages();
		}

		void filters_OnFiltersListChanged(object sender, EventArgs e)
		{
			updates.InvalidateFilters();
			updates.InvalidateMessages();
		}

	}
}
