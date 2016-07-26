using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogJoint.MessagesContainers;

namespace LogJoint
{
	// todo: split to files per class
	// todo: handle log sources switch on/off/closing
	public interface ISearchManager
	{
		// ISearchResult ActiveSearch { get; }
		ISearchResult SubmitSearch(Search.Options options);
		IEnumerable<ISearchResult> Results { get; }

		/// <summary>
		/// Occurs when the list of search results changed.
		/// </summary>
		event EventHandler SearchResultsChanged;
		/// <summary>
		/// Occurs when search result changed. Is fired from random thread. Can be very frequent.
		/// Do not do expensive computations in the handler.
		/// </summary>
		event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;
	};

	public interface ISearchResult
	{
		IEnumerable<ISourceSearchResult> Results { get; }
		Search.Options Options { get; }
		int HitsCount { get; }
		double? Progress { get; }
		bool Visible { get; set; }
		bool Pinned { get; set; }
	};

	internal interface ISearchResultInternal: ISearchResult
	{
		void StartSearch(ILogSourcesManager sources);
		void Cancel();
		void OnResultChanged(ISourceSearchResult rslt);
	};

	internal interface ISearchObjectsFactory
	{
		ISearchResultInternal CreateSearchResults(ISearchManagerInternal owner, Search.Options options);
		ISourceSearchResultInternal CreateSourceSearchResults(ILogSource source, ISearchResultInternal owner);
	};

	internal interface ISearchManagerInternal: ISearchManager
	{
		void OnResultChanged(ISourceSearchResult rslt);
	};

	internal class SearchObjectsFactory: ISearchObjectsFactory
	{
		ISearchResultInternal ISearchObjectsFactory.CreateSearchResults (
			ISearchManagerInternal owner, Search.Options options)
		{
			return new SearchResult(owner, options, this);
		}
		ISourceSearchResultInternal ISearchObjectsFactory.CreateSourceSearchResults (ILogSource source, ISearchResultInternal owner)
		{
			return new SourceSearchResult(source, owner);
		}
	};

	public interface ISourceSearchResult
	{
		ILogSource Source { get; }
		ISearchResult Parent { get; }
		DateBoundPositionResponseData GetDateBoundPosition(
			DateTime d,
			ListUtils.ValueBound bound
		);
		void EnumMessages(
			long fromPosition,
			Func<IndexedMessage, bool> callback,
			EnumMessagesFlag flags
		);
		FileRange.Range PositionsRange { get; }
		DateRange? DatesRange { get; }
		FileRange.Range? IndexesRange { get; }
	};

	internal interface ISourceSearchResultInternal: ISourceSearchResult
	{
		void StartTask(Search.Options options, CancellationToken cancellation);
	};

	[Flags]
	public enum SearchResultChangeFlag
	{
		None = 0,
		MessagesChanged = 1,
		HitsCountChanged = 2,
		ProgressChanged = 4,
		VisibleChanged = 8,
		PinnedChanged = 16,
	};

	public class SearchResultChangeEventArgs
	{
		public SearchResultChangeFlag Flags { get; private set; }

		public SearchResultChangeEventArgs(SearchResultChangeFlag flags)
		{
			this.Flags = flags;
		}
	};

	public class SearchManager: ISearchManager, ISearchManagerInternal
	{
		readonly ILogSourcesManager sources;
		readonly ISearchObjectsFactory factory;
		readonly List<ISearchResultInternal> results = new List<ISearchResultInternal>();

		public SearchManager(ILogSourcesManager sources): 
			this(sources, new SearchObjectsFactory())
		{
		}

		internal SearchManager(
			ILogSourcesManager sources,
			ISearchObjectsFactory factory
		)
		{
			this.sources = sources;
			this.factory = factory;
		}

		public event EventHandler SearchResultsChanged;
		public event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;

		ISearchResult ISearchManager.SubmitSearch(Search.Options options)
		{
			var result = factory.CreateSearchResults(this, options);
			result.StartSearch(sources);
			results.RemoveAll(r => !r.Pinned); // todo: cancel active searches
			results.Add(result);
			if (SearchResultsChanged != null)
				SearchResultsChanged(this, EventArgs.Empty);
			return result;
		}

		IEnumerable<ISearchResult> ISearchManager.Results
		{
			get { return results; }
		}

		void ISearchManagerInternal.OnResultChanged(ISourceSearchResult rslt)
		{
			var changeFlags = SearchResultChangeFlag.None;
			changeFlags |= SearchResultChangeFlag.MessagesChanged;
			if (SearchResultChanged != null)
			{
				SearchResultChanged(rslt, new SearchResultChangeEventArgs(changeFlags));
			}
		}
	};

	class SearchResult: ISearchResultInternal
	{
		readonly ISearchObjectsFactory factory;
		readonly ISearchManagerInternal owner;
		readonly Search.Options options;
		readonly CancellationTokenSource cancellation; // todo: cancellation
		readonly List<ISourceSearchResultInternal> results;

		public SearchResult(
			ISearchManagerInternal owner,
			Search.Options options,
			ISearchObjectsFactory factory
		)
		{
			this.owner = owner;
			this.options = options;
			this.factory = factory;
			this.cancellation = new CancellationTokenSource();
			this.results = new List<ISourceSearchResultInternal>();
		}

		IEnumerable<ISourceSearchResult> ISearchResult.Results
		{
			get { return results; }
		}

		Search.Options ISearchResult.Options 
		{
			get { return options; }
		}

		int ISearchResult.HitsCount {
			get {
				throw new NotImplementedException ();
			}
		}

		double? ISearchResult.Progress {
			get {
				throw new NotImplementedException ();
			}
		}

		bool ISearchResult.Visible {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		bool ISearchResult.Pinned
		{
			get
			{
				return false;
			}
			set
			{
				throw new NotImplementedException ();
			}
		}

		void ISearchResultInternal.StartSearch(ILogSourcesManager sources)
		{
			var sourcesResults = sources.Items.Select(
				src => factory.CreateSourceSearchResults(src, this)).ToList();
			sourcesResults.ForEach(r => r.StartTask(options, cancellation.Token));
			results.AddRange(sourcesResults);
		}

		void ISearchResultInternal.Cancel()
		{
			cancellation.Cancel();
		}

		void ISearchResultInternal.OnResultChanged(ISourceSearchResult rslt)
		{
			// todo: update cumulative props such as completion percentange, hits
			owner.OnResultChanged(rslt);
		}
	};

	class SourceSearchResult: ISourceSearchResultInternal
	{
		readonly ILogSource source;
		readonly ISearchResultInternal parent;
		readonly List<IMessage> messages;
		readonly object messagesLock = new object();
		Task task;

		public SourceSearchResult(ILogSource src, ISearchResultInternal parent)
		{
			this.source = src;
			this.parent = parent;
			this.messages = new List<IMessage>();
		}

		DateBoundPositionResponseData ISourceSearchResult.GetDateBoundPosition (DateTime d, ListUtils.ValueBound bound)
		{
			lock (messagesLock)
			{
				var idx = ListUtils.GetBound(messages, (IMessage)null, bound, new DatesComparer() { d = d });
				if (idx < 0)
					return new DateBoundPositionResponseData()
					{
						Position = -1,
						Index = -1,
						IsBeforeBeginPosition = true,
					};
				if (idx >= messages.Count)
					return new DateBoundPositionResponseData()
					{
						Position = messages.Count == 0 ? 0 : (messages[messages.Count - 1].Position + 1),
						Index = messages.Count,
						IsEndPosition = true,
					};
				return new DateBoundPositionResponseData()
				{
					Position = messages[idx].Position,
					Index = idx,
					Date = messages[idx].Time
				};
			}
		}

		void ISourceSearchResult.EnumMessages (long fromPosition, Func<IndexedMessage, bool> callback, EnumMessagesFlag flags)
		{
			var forward = (flags & EnumMessagesFlag.Forward) != 0;
			lock (messagesLock)
			{
				var idx = ListUtils.GetBound(messages, null, 
					forward ? ListUtils.ValueBound.Lower : ListUtils.ValueBound.UpperReversed, 
					new PositionsComparer() { p = fromPosition });
				if (forward)
				{
					for (; idx < messages.Count; ++idx)
						if (!callback(new IndexedMessage(idx, messages[idx])))
							return;
				}
				else
				{
					for (; idx >= 0; --idx)
						if (!callback(new IndexedMessage(idx, messages[idx])))
							return;
				}
			}
		}

		ILogSource ISourceSearchResult.Source
		{
			get { return source; }
		}

		ISearchResult ISourceSearchResult.Parent
		{
			get { return parent; }
		}

		FileRange.Range ISourceSearchResult.PositionsRange
		{
			get 
			{
				lock (messagesLock)
				{
					if (messages.Count == 0)
						return new FileRange.Range();
					return new FileRange.Range(messages[0].Position, messages[messages.Count - 1].Position + 1);
				}
			}
		}

		DateRange? ISourceSearchResult.DatesRange 
		{
			get
			{
				lock (messagesLock)
				{
					if (messages.Count == 0)
						return null;
					return new DateRange(messages[0].Time.ToUnspecifiedTime(), 
						messages[messages.Count - 1].Time.ToUnspecifiedTime().AddTicks(1));
				}
			}
		}

		FileRange.Range? ISourceSearchResult.IndexesRange
		{
			get
			{
				lock (messagesLock)
				{
					return new FileRange.Range(0, messages.Count);
				}
			}
		}


		void ISourceSearchResultInternal.StartTask(Search.Options options, CancellationToken cancellation)
		{
			task = Worker(options, cancellation);
		}

		async Task Worker(Search.Options options, CancellationToken cancellation)
		{
			await source.Provider.Search(
				new SearchAllOccurencesParams(options, 0), // todo: pass current position from search presenter
				msg =>
				{
					lock (messagesLock)
					{
						messages.Add(msg); // todo: handle ooo messages somehow. ignore?
					}
					parent.OnResultChanged(this);
					return true;
				},
				cancellation
			);
		}

		class DatesComparer: IComparer<IMessage>
		{
			public DateTime d;

			int IComparer<IMessage>.Compare (IMessage x, IMessage y)
			{
				var d1 = x == null ? d : x.Time.ToUnspecifiedTime();
				var d2 = y == null ? d : y.Time.ToUnspecifiedTime();
				return DateTime.Compare(d1, d2);
			}
		};

		class PositionsComparer: IComparer<IMessage>
		{
			public long p;

			int IComparer<IMessage>.Compare (IMessage x, IMessage y)
			{
				var p1 = x == null ? p : x.Position;
				var p2 = y == null ? p : y.Position;
				return Math.Sign(p1 - p2);
			}
		};
	};
}
