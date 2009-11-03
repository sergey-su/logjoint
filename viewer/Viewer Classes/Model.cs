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

		void OnUpdateView();

		bool FocusRectIsRequired { get; }
		IUINavigationHandler UINavigationHandler { get; }

		IMainForm MainFormObject { get; }
	};

	public class Model: 
		IFactoryUICallback,
		IThreadsEvents, 
		IBookmarksEvents,
		ILogSourcesManagerHost,
		IFiltersEvents,
		UI.ILogViewerControlHost,
		UI.ITimeLineControlHost,
		UI.IThreadsListViewHost,
		UI.ISourcesListViewHost,
		UI.IFiltersListViewHost
	{
		readonly Source tracer;
		readonly IModelHost host;
		readonly UpdateTracker updates;
		readonly LogSourcesManager logSources;
		readonly Threads threads;
		readonly Bookmarks bookmarks;
		readonly SourcesCollection sourcesCollection;
		readonly FiltersList filters;


		public Model(IModelHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;
			updates = new UpdateTracker();
			threads = new Threads(this);
			logSources = new LogSourcesManager(this);
			sourcesCollection = new SourcesCollection(logSources.Items);
			bookmarks = new Bookmarks(this);
			filters = new FiltersList(this);
		}

		public Source Tracer { get { return tracer; } }

		public UpdateTracker Updates { get { return updates; } }

		public Bookmarks Bookmarks { get { return bookmarks; } }

		public void DeleteLogs(ILogSource[] logs)
		{
			foreach (ILogSource s in logs)
				s.Dispose();
			updates.InvalidateSources();
			updates.InvalidateMessages();
			updates.InvalidateTimeLine();
		}

		public void DeleteLogs()
		{
			DeleteLogs(new List<ILogSource>(logSources.Items).ToArray());
		}

		public ILogReader LoadFrom(RecentLogEntry entry)
		{
			ILogSource src = null;
			ILogReader reader = null;
			try
			{
				reader = FindExistingReader(entry.ConnectionParams);
				if (reader != null)
					return reader;
				src = logSources.Create();
				reader = entry.Factory.CreateFromConnectionParams(src, entry.ConnectionParams);
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
			host.OnNewReader(reader);
		}

		public ILogReader FindExistingReader(IConnectionParams connectParams)
		{
			ILogSource s = logSources.Find(connectParams);
			if (s == null)
				return null;
			return s.Reader;
		}

		#endregion

		#region IThreadsEvents Members

		public void OnThreadListChanged()
		{
			updates.InvalidateThreads();
		}

		public void OnThreadVisibilityChanged(IThread t)
		{
			updates.InvalidateThreads();
			updates.InvalidateMessages();
		}

		public void OnPropertiesChanged(IThread t)
		{
			updates.InvalidateThreads();
		}

		#endregion

		#region IBookmarksEvents

		public void OnBookmarksChanged()
		{
			updates.InvalidateTimeLine();
			updates.InvalidateMessages();
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

		public FiltersList Filters 
		{
			get { return filters; } 
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

		#endregion

		#region ISourcesListViewHost Members

		IEnumerable<ILogSource> UI.ISourcesListViewHost.LogSources
		{
			get { return logSources.Items; }
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

		#region IFiltersEvents Members

		public void OnFiltersListChanged()
		{
			updates.InvalidateFilters();
			updates.InvalidateMessages();
		}

		public void OnPropertiesChanged(Filter f, bool changeAffectsFilterResult)
		{
			updates.InvalidateFilters();
			if (changeAffectsFilterResult)
				updates.InvalidateMessages();
		}

		#endregion

		#region IFiltersListViewHost Members

		FiltersList UI.IFiltersListViewHost.Filters
		{
			get { return filters; }
		}

		IEnumerable<ILogSource> UI.IFilterDialogHost.LogSources
		{
			get { return logSources.Items; }
		}

		#endregion
	}
}
