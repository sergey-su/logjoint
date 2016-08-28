using System;
using System.Collections.Generic;

namespace LogJoint
{
	public interface ISearchManager
	{
		// ISearchResult ActiveSearch { get; }
		ISearchResult SubmitSearch(SearchAllOptions options);
		IEnumerable<ISearchResult> Results { get; }

		/// <summary>
		/// Occurs when the list of Results collection changed.
		/// </summary>
		event EventHandler SearchResultsChanged;
		/// <summary>
		/// Occurs when one search result changed. It's fired from random thread. Can be very frequent.
		/// Do not do expensive computations in the handler.
		/// </summary>
		event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;
	};

	public class SearchAllOptions
	{
		public Search.Options CoreOptions;
		public Dictionary<ILogSource, long> StartPositions;
	};

	public enum SearchResultStatus
	{
		Active,
		Finished,
		Cancelled,
		HitLimitReached,
		Failed
	};

	public interface ISearchResult
	{
		/// <summary>
		/// Unique in scope of LogJoint's process lifetime.
		/// Nonotonically incremented: newer searches have bigger id than older ones.
		/// </summary>
		int Id { get; }
		SearchResultStatus Status { get; }
		IEnumerable<ISourceSearchResult> Results { get; }
		SearchAllOptions Options { get; }
		int HitsCount { get; }
		double? Progress { get; }
		bool Visible { get; set; }
		bool Pinned { get; set; }
		void Cancel();
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
			Func<IMessage, bool> callback,
			EnumMessagesFlag flags
		);
		FileRange.Range PositionsRange { get; }
		DateRange DatesRange { get; }

		FileRange.Range SequentialPositionsRange { get; }
		long MapMessagePositionToSequentialPosition(long pos);
		long MapSequentialPositionToMessagePosition(long pos);
	};

	[Flags]
	public enum SearchResultChangeFlag
	{
		None = 0,
		StatusChanged = 1,
		ResultsCollectionChanges = 2,
		MessagesChanged = 4,
		ProgressChanged = 8,
		VisibleChanged = 16,
		PinnedChanged = 32,
	};

	public class SearchResultChangeEventArgs
	{
		public SearchResultChangeFlag Flags { get; private set; }

		public SearchResultChangeEventArgs(SearchResultChangeFlag flags)
		{
			this.Flags = flags;
		}
	};
}
