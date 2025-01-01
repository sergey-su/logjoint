using System.Threading;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace LogJoint
{
    internal interface ISearchResultInternal : ISearchResult, IDisposable
    {
        /// <summary>
        /// List of search results in each log source.
        /// The collection includes results 
        /// from invisible and disposed sources.
        /// Called from model thread.
        /// </summary>
        new IEnumerable<ISourceSearchResultInternal> Results { get; }
        /// <summary>
        /// SourceSearchResult objects calls this method
        /// to notify its owner about changes in its messages collection.
        /// Called from thread pool thread.
        /// </summary>
        void OnResultChanged(ISourceSearchResultInternal rslt);
        /// <summary>
        /// SourceSearchResult objects calls this method
        /// to notify its owner about search finish.
        /// Called from model thread.
        /// </summary>
        void OnResultCompleted(ISourceSearchResultInternal rslt);
        /// <summary>
        /// SourceSearchResult asks its owner permission to add new message 
        /// to its messages collection.
        /// Called from thread pool thread.
        /// </summary>
        bool AboutToAddNewMessage();
        /// <summary>
        /// Called when log source swithed on or off.
        /// Called from model thread.
        /// </summary>
        void FireChangeEventIfContainsSourceResults(ILogSource source);
    };

    internal interface ISearchObjectsFactory
    {
        ISearchResultInternal CreateSearchResults(ISearchManagerInternal owner, SearchAllOptions options, IFilter optionsFilter,
            int id, IList<ILogSourceSearchWorkerInternal> workers);
        ISourceSearchResultInternal CreateSourceSearchResults(ILogSourceSearchWorkerInternal searchWorker,
            ISearchResultInternal owner, CancellationToken cancellation, Progress.IProgressAggregator progress);
        ICombinedSearchResultInternal CreateCombinedSearchResult(ISearchManagerInternal owner);
        ICombinedSourceSearchResultInternal CreateCombinedSourceSearchResult(ILogSource source);
        ILogSourceSearchWorkerInternal CreateSearchWorker(ILogSource forSource, SearchAllOptions options);
    };

    internal interface ISearchManagerInternal : ISearchManager
    {
        void OnResultChanged(ISearchResult rslt, SearchResultChangeFlag flags);
    };

    internal interface ISourceSearchResultInternal : ISourceSearchResult, IDisposable
    {
        SearchResultStatus Status { get; }
        void ReleaseProgress();
        MessagesContainers.ListBasedCollection CreateMessagesSnapshot();
        MessagesContainers.ListBasedCollection GetLastSnapshot();
    };

    internal interface ICombinedSearchResultInternal : ICombinedSearchResult
    {
        void Init(ISourceSearchResultInternal[] results, CancellationToken cancellation);
    };

    internal interface ICombinedSourceSearchResultInternal : ICombinedSourceSearchResult
    {
        bool Add(IMessage msg);
    };

    internal interface ILogSourceSearchWorkerInternal
    {
        ILogSource LogSource { get; }
        Task GetMessages(IFilter filter, Func<SearchResultMessage, bool> callback,
            CancellationToken cancellation, Progress.IProgressEventsSink progressSink);
        void Start();
    };
}
