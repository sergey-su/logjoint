using System;
using System.Collections.Generic;

namespace LogJoint
{
    /// <summary>
    /// Manages active search and searches history.
    /// Threading: see individual members.
    /// </summary>
    public interface ISearchManager
    {
        /// <summary>
        /// Starts a new search.
        /// Must be called from model thread.
        /// </summary>
        void SubmitSearch(SearchAllOptions options);
        /// <summary>
        /// Lists active and historical search results.
        /// Must be called from model thread.
        /// </summary>
        IReadOnlyList<ISearchResult> Results { get; }
        /// <summary>
        /// Get current snapshot of merged search results.
        /// Only visible search results from visible log sources
        /// are included to the snapshot.
        /// Reference to same object is returned until 
        /// combined search result changes.
        /// Must be called from model thread.
        /// </summary>
        ICombinedSearchResult CombinedSearchResult { get; }
        /// <summary>
        /// Deletes given search result 
        /// </summary>
        void Delete(ISearchResult rslt);

        /// <summary>
        /// Occurs when the Results collection changes.
        /// Fired from model thread.
        /// </summary>
        event EventHandler SearchResultsChanged;
        /// <summary>
        /// Occurs when individual ISearchResult changes. It's fired from a thread pool thread. 
        /// Can be very frequent. Do not do expensive computations in the handler.
        /// </summary>
        event EventHandler<SearchResultChangeEventArgs> SearchResultChanged;
        /// <summary>
        /// Occurs when CombinedSearchResult changes.
        /// Fired from an thread pool thread.
        /// </summary>
        event EventHandler CombinedSearchResultChanged;
    };

    public class SearchAllOptions
    {
        public IFiltersList Filters;
        public bool SearchInRawText;
        public Dictionary<ILogSource, long> StartPositions;
        public string SearchName;
    };

    public enum SearchResultStatus
    {
        Active,
        Finished,
        Cancelled,
        HitLimitReached,
        Failed
    };

    /// <summary>
    /// Represents one search attempt by the user.
    /// All members must be called from model thread.
    /// </summary>
    public interface ISearchResult
    {
        /// <summary>
        /// Numeric id. Unique in scope of LogJoint's process lifetime.
        /// Monotonically incremented: newer searches have bigger ids than older ones.
        /// </summary>
        int Id { get; }
        SearchResultStatus Status { get; }
        IEnumerable<ISourceSearchResult> Results { get; }
        SearchAllOptions Options { get; }
        IFilter OptionsFilter { get; }
        int HitsCount { get; }
        double? Progress { get; }
        /// <summary>
        /// Indicates whether search result is merged into combined search result by SearchManager.
        /// Combined search result is displayed to the user.
        /// </summary>
        bool Visible { get; set; }
        bool Pinned { get; set; }
        /// <summary>
        /// Indicates whether this search result should be displayed on timeline.
        /// </summary>
        bool VisibleOnTimeline { get; set; }
        void Cancel();

        DateRange CoveredTime { get; }
        ITimeGapsDetector TimeGaps { get; }
    };

    public interface ISourceSearchResult
    {
        ILogSource Source { get; }
        int HitsCount { get; }
    };

    /// <summary>
    /// Provides access to search results snapshot
    /// </summary>
    public interface ICombinedSearchResult
    {
        IList<ICombinedSourceSearchResult> Results { get; }
    };

    public interface ICombinedSourceSearchResult
    {
        ILogSource Source { get; }
        FileRange.Range PositionsRange { get; }
        DateRange DatesRange { get; }
        void EnumMessages(
            long fromPosition,
            Func<IMessage, bool> callback,
            EnumMessagesFlag flags
        );
        DateBoundPositionResponseData GetDateBoundPosition(
            DateTime d,
            ValueBound bound
        );

        FileRange.Range SequentialPositionsRange { get; }
        long MapMessagePositionToSequentialPosition(long pos);
        long MapSequentialPositionToMessagePosition(long pos);
    };


    [Flags]
    public enum SearchResultChangeFlag
    {
        None = 0,
        StatusChanged = 1,
        ResultsCollectionChanged = 2,
        HitCountChanged = 4,
        ProgressChanged = 8,
        VisibleChanged = 16,
        PinnedChanged = 32,
        VisibleOnTimelineChanged = 64,
        TimeGapsChanged = 128,
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
