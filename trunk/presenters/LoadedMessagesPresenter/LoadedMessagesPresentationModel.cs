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
		readonly List<MessagesSource> sources = new List<MessagesSource>();
		readonly AsyncInvokeHelper updateSourcesInvoker;

		public PresentationModel(
			ILogSourcesManager logSources,
			ISynchronizationContext modelInvoke,
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

			updateSourcesInvoker = new AsyncInvokeHelper(modelInvoke, UpdateSources);

			logSources.OnLogSourceColorChanged += (s, e) =>
			{
				if (OnLogSourceColorChanged != null)
					OnLogSourceColorChanged(s, e);
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
					if ((e.Flags & LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag) == 0)
						updateSourcesInvoker.Invoke();
					else if (OnSourceMessagesChanged != null)
						OnSourceMessagesChanged(this, EventArgs.Empty);
				}
			};
		}

		public event EventHandler OnSourcesChanged;
		public event EventHandler OnSourceMessagesChanged;
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
			var impl = src as MessagesSource;
			if (impl == null)
				return null;
			return impl.ls;
		}

		void UpdateSources()
		{
			var newSources = logSources.Items.Where(
				s => !s.IsDisposed && s.Visible && s.Provider.Stats.PositionsRangeUpdatesCount > 0).ToHashSet();
			int removed = sources.RemoveAll(s => !newSources.Contains(s.ls));
			sources.ForEach(s => newSources.Remove(s.ls));
			sources.AddRange(newSources.Select(s => new MessagesSource() { ls = s } ));
			var added = newSources.Count;
			if ((removed + added) > 0 && OnSourcesChanged != null)
				OnSourcesChanged(this, EventArgs.Empty);
		}

		class MessagesSource: IMessagesSource
		{
			public ILogSource ls;

			Task<DateBoundPositionResponseData> IMessagesSource.GetDateBoundPosition (DateTime d, ValueBound bound, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				return ls.Provider.GetDateBoundPosition(d, bound, false, priority, cancellation);
			}

			Task IMessagesSource.EnumMessages (long fromPosition, Func<IMessage, bool> callback, 
				EnumMessagesFlag flags, LogProviderCommandPriority priority, CancellationToken cancellation)
			{
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