using System.Threading;
using System.Collections.Generic;

namespace LogJoint
{
	internal interface ISearchResultInternal : ISearchResult
	{
		/// <summary>
		/// Instructs newly created SearchResult to initiate
		/// search in each log source 
		/// present in passed sources manager object.
		/// </summary>
		void StartSearch(ILogSourcesManager sources);
		/// <summary>
		/// List of search results in each log source.
		/// The collection includes results 
		/// from invisible and disposed sources.
		/// Called from model thread.
		/// </summary>
		IEnumerable<ISourceSearchResultInternal> Results { get; }
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
		ISearchResultInternal CreateSearchResults(ISearchManagerInternal owner, SearchAllOptions options, int id);
		ISourceSearchResultInternal CreateSourceSearchResults(ILogSource source, ISearchResultInternal owner);
		ICombinedSearchResultInternal CreateCombinedSearchResult(ISearchManagerInternal owner);
		ICombinedSourceSearchResultInternal CreateCombinedSourceSearchResult(ILogSource source);
	};

	internal interface ISearchManagerInternal : ISearchManager
	{
		void OnResultChanged(ISearchResult rslt, SearchResultChangeFlag flags);
	};

	internal interface ISourceSearchResultInternal: ISourceSearchResult
	{
		void StartTask(SearchAllOptions options, CancellationToken cancellation, Progress.IProgressAggregator progress);
		SearchResultStatus Status { get; }
		MessagesContainers.ListBasedCollection CreateMessagesSnapshot();
		MessagesContainers.ListBasedCollection GetLastSnapshot();
	};

	internal interface ICombinedSearchResultInternal: ICombinedSearchResult
	{
		void Init(ISourceSearchResultInternal[] results, CancellationToken cancellation);
	};

	internal interface ICombinedSourceSearchResultInternal : ICombinedSourceSearchResult
	{
		bool Add(IMessage msg);
	};
}
