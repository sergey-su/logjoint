using System.Threading;

namespace LogJoint
{
	internal interface ISearchResultInternal : ISearchResult
	{
		void StartSearch(ILogSourcesManager sources);
		void OnResultChanged(ISourceSearchResultInternal rslt);
		void OnResultCompleted(ISourceSearchResultInternal rslt);
		bool AboutToAddNewMessage();
		void FireChangeEventIfContainsSourceResults(ILogSource source);
	};

	internal interface ISearchObjectsFactory
	{
		ISearchResultInternal CreateSearchResults(ISearchManagerInternal owner, SearchAllOptions options, int id);
		ISourceSearchResultInternal CreateSourceSearchResults(ILogSource source, ISearchResultInternal owner);
	};

	internal interface ISearchManagerInternal : ISearchManager
	{
		void OnResultChanged(ISearchResult rslt, SearchResultChangeFlag flags);
	};

	internal interface ISourceSearchResultInternal : ISourceSearchResult
	{
		void StartTask(SearchAllOptions options, CancellationToken cancellation, Progress.IProgressAggregator progress);
		SearchResultStatus Status { get; }
		int HitsCount { get; }
	};
}
