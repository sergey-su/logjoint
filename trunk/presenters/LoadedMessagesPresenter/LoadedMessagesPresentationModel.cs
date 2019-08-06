using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.LogViewer;
using System.Threading;

namespace LogJoint.UI.Presenters.LoadedMessages
{
	public class PresentationModel : LogViewer.IModel
	{
		readonly ILogSourcesManager logSources;
		readonly IModelThreads modelThreads;
		readonly IFiltersList hlFilters;
		readonly IBookmarks bookmarks;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly ISynchronizationContext synchronizationContext;
		readonly List<MessagesSource> sources = new List<MessagesSource>();
		readonly AsyncInvokeHelper updateSourcesInvoker;

		public PresentationModel(
			ILogSourcesManager logSources,
			ISynchronizationContext sync,
			IModelThreads modelThreads,
			IFiltersList hlFilters,
			IBookmarks bookmarks,
			Settings.IGlobalSettingsAccessor settings
		)
		{
			this.logSources = logSources;
			this.modelThreads = modelThreads;
			this.hlFilters = hlFilters;
			this.bookmarks = bookmarks;
			this.settings = settings;
			this.synchronizationContext = sync;

			updateSourcesInvoker = new AsyncInvokeHelper(sync, UpdateSources);

			logSources.OnLogSourceColorChanged += (s, e) =>
			{
				OnLogSourceColorChanged?.Invoke(s, e);
			};
			logSources.OnLogSourceAdded += (s, e) =>
			{
				updateSourcesInvoker.Invoke();
			};
			logSources.OnLogSourceRemoved += (s, e) =>
			{
				updateSourcesInvoker.Invoke();
			};
			logSources.OnLogSourceStatsChanged += (s, e) =>
			{
				if ((e.Flags & LogProviderStatsFlag.PositionsRange) != 0)
				{
					if ((e.Flags & LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag) != 0)
					{
						FireMessagesChanged(s, isIncrementalChange: true);
					}
					else if (IsExposableLogSource(e.Value) && !IsExposableLogSource(e.OldValue))
					{
						updateSourcesInvoker.Invoke();
					}
					else
					{
						FireMessagesChanged(s, isIncrementalChange: false);
					}
				}
			};
		}

		public event EventHandler OnSourcesChanged;
		public event EventHandler<SourceMessagesChangeArgs> OnSourceMessagesChanged;
		public event EventHandler OnLogSourceColorChanged;

		IEnumerable<IMessagesSource> LogViewer.IModel.Sources
		{
			get { return sources; }
		}

		IModelThreads LogViewer.IModel.Threads
		{
			get { return modelThreads; }
		}

		IFiltersList LogViewer.IModel.HighlightFilters
		{
			get { return hlFilters; }
		}

		IBookmarks LogViewer.IModel.Bookmarks
		{
			get { return bookmarks; }
		}

		string LogViewer.IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get
			{
				return "No log sources open. To add new log source:\n  - Press Add... button on Log Sources tab\n  - or drag&&drop (possibly zipped) log file from Windows Explorer\n  - or drag&&drop URL from a browser to download (possibly zipped) log file";
			}
		}

		Settings.IGlobalSettingsAccessor LogViewer.IModel.GlobalSettings
		{
			get { return settings; }
		}

		static public ILogSource MessagesSourceToLogSource(IMessagesSource src)
		{
			if (src is MessagesSource impl)
				return impl.ls;
			return null;
		}

		static bool IsExposableLogSource(LogProviderStats logProviderStats)
		{
			return logProviderStats != null && logProviderStats.PositionsRangeUpdatesCount > 0;
		}

		void UpdateSources()
		{
			var newSources = logSources.VisibleItems.Where(
				s => IsExposableLogSource(s.Provider.Stats)).ToHashSet();
			int removed = sources.RemoveAll(s => !newSources.Contains(s.ls));
			sources.ForEach(s => newSources.Remove(s.ls));
			sources.AddRange(newSources.Select(s => new MessagesSource() { ls = s } ));
			var added = newSources.Count;
			if ((removed + added) > 0 && OnSourcesChanged != null)
				OnSourcesChanged(this, EventArgs.Empty);
		}

		void FireMessagesChanged(object logSource, bool isIncrementalChange)
		{
			synchronizationContext.Post(() =>
				OnSourceMessagesChanged?.Invoke(logSource, new SourceMessagesChangeArgs(isIncrementalChange)));
		}

		class MessagesSource: IMessagesSource
		{
			public ILogSource ls;

			Task<DateBoundPositionResponseData> IMessagesSource.GetDateBoundPosition (DateTime d, ValueBound bound, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				if (ls.IsDisposed)
					throw new OperationCanceledException();
				return ls.Provider.GetDateBoundPosition(d, bound, false, priority, cancellation);
			}

			Task IMessagesSource.EnumMessages (long fromPosition, Func<IMessage, bool> callback, 
				EnumMessagesFlag flags, LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				if (ls.IsDisposed)
					throw new OperationCanceledException();
				return ls.Provider.EnumMessages(fromPosition, callback, flags, priority, cancellation);
			}

			FileRange.Range IMessagesSource.PositionsRange
			{
				get { return !ls.Provider.IsDisposed ? ls.Provider.Stats.PositionsRange : new FileRange.Range(); }
			}

			DateRange IMessagesSource.DatesRange
			{
				get { return !ls.Provider.IsDisposed ? ls.Provider.Stats.AvailableTime : new DateRange(); }
			}

			FileRange.Range LogViewer.IMessagesSource.ScrollPositionsRange
			{
				get { return !ls.Provider.IsDisposed ? ls.Provider.Stats.PositionsRange : new FileRange.Range(); }
			}

			long LogViewer.IMessagesSource.MapPositionToScrollPosition(long pos)
			{
				return pos;
			}

			long LogViewer.IMessagesSource.MapScrollPositionToPosition(long pos)
			{
				return pos;
			}

			ILogSource LogViewer.IMessagesSource.LogSourceHint
			{
				get { return ls; }
			}

			bool LogViewer.IMessagesSource.HasConsecutiveMessages
			{
				get { return true; }
			}
		};
	};
};
