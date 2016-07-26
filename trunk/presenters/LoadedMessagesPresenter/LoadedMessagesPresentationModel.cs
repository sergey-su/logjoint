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
		readonly IModel model;
		readonly List<MessagesSource> sources = new List<MessagesSource>();

		readonly IFiltersList testFilters;

		public PresentationModel(IModel model)
		{
			this.model = model;

			IFiltersFactory ff = new FiltersFactory();
			testFilters = ff.CreateFiltersList(FilterAction.Exclude);
			testFilters.Insert(0, ff.CreateFilter(FilterAction.Include, "foobar", true, "NGStrand", false, false, false));

			this.model.SourcesManager.OnLogSourceColorChanged += (s, e) =>
			{
				if (OnLogSourceColorChanged != null)
					OnLogSourceColorChanged(s, e);
			};
			this.model.SourcesManager.OnLogSourceAdded += (s, e) =>
			{
				UpdateSources();
			};
			this.model.SourcesManager.OnLogSourceRemoved += (s, e) =>
			{
				UpdateSources();
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
			get { return model.Threads; }
		}

		IFiltersList LogViewer.IModel.HighlightFilters
		{
			//get { return testFilters; }
			get { return model.HighlightFilters; }
		}

		IBookmarks LogViewer.IModel.Bookmarks
		{
			get { return model.Bookmarks; }
		}

		string LogViewer.IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get
			{
				if (model.SourcesManager.Items.Any(s => s.Visible)) // todo: need this check?
					return null;
				return "No log sources open. To add new log source:\n  - Press Add... button on Log Sources tab\n  - or drag&&drop (possibly zipped) log file from Windows Explorer\n  - or drag&&drop URL from a browser to download (possibly zipped) log file";
			}
		}

		Settings.IGlobalSettingsAccessor LogViewer.IModel.GlobalSettings
		{
			get { return model.GlobalSettings; }
		}

		void UpdateSources()
		{
			var newSources = model.SourcesManager.Items.Where(
				s => !s.IsDisposed && s.Visible).ToHashSet();
			sources.RemoveAll(s => !newSources.Contains(s.ls));
			sources.ForEach(s => newSources.Remove(s.ls));
			sources.AddRange(newSources.Select(s => new MessagesSource() { ls = s } ));
			if (OnSourcesChanged != null)
				OnSourcesChanged(this, EventArgs.Empty);
		}

		class MessagesSource: IMessagesSource
		{
			public ILogSource ls;

			Task<DateBoundPositionResponseData> IMessagesSource.GetDateBoundPosition (DateTime d, ListUtils.ValueBound bound, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				return ls.Provider.GetDateBoundPosition(d, bound, priority, cancellation);
			}

			Task IMessagesSource.EnumMessages (long fromPosition, Func<IndexedMessage, bool> callback, 
				EnumMessagesFlag flags, LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				return ls.Provider.EnumMessages(fromPosition, 
					m => callback(new IndexedMessage(-1, m)), flags, priority, cancellation);
			}

			FileRange.Range IMessagesSource.PositionsRange
			{
				// todo: how to return consistent PositionsRange and DatesRange when stats are concurrently changed?
				get { return ls.Provider.Stats.PositionsRange.GetValueOrDefault(new FileRange.Range()); }
			}

			DateRange? IMessagesSource.DatesRange
			{
				get { return ls.Provider.Stats.AvailableTime; }
			}

			FileRange.Range? IMessagesSource.IndexesRange
			{
				get { return null; }
			}
		};
	};
};