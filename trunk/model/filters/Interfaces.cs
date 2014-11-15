using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public enum FilterAction
	{
		Include = 0,
		Exclude = 1
	};

	public interface IFiltersList : IDisposable
	{
		int PurgeDisposedFiltersAndFiltersHavingDisposedThreads();

		FiltersBulkProcessingHandle BeginBulkProcessing();
		void EndBulkProcessing(FiltersBulkProcessingHandle handle);

		FilterAction ProcessNextMessageAndGetItsAction(MessageBase msg, FiltersPreprocessingResult preprocessingResult, FilterContext filterCtx, bool matchRawMessages);
		FilterAction ProcessNextMessageAndGetItsAction(MessageBase msg, FilterContext filterCtx, bool matchRawMessages);
		FiltersPreprocessingResult PreprocessMessage(MessageBase msg, bool matchRawMessages);

		IFiltersList Clone();
		bool FilteringEnabled { get; set; }
		void Insert(int position, IFilter filter);
		void Delete(IEnumerable<IFilter> range);
		bool Move(IFilter f, bool upward);
		IEnumerable<IFilter> Items { get; }
		int Count { get; }
		FilterAction GetDefaultAction();
		int GetDefaultActionCounter();

		event EventHandler OnFiltersListChanged;
		event EventHandler OnFilteringEnabledChanged;
		event EventHandler<FilterChangeEventArgs> OnPropertiesChanged;
		event EventHandler OnCountersChanged;

		void InvalidateDefaultAction();
		void FireOnPropertiesChanged(IFilter sender, bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult);
	};

	public struct FiltersPreprocessingResult
	{
		internal UInt64 mask;

		internal const int MaxEnabledFiltersSupportedByPreprocessing = 64;
	};

	public class FiltersBulkProcessingHandle
	{
		internal int[] counters;
	};

	public class FilterChangeEventArgs: EventArgs
	{
		public FilterChangeEventArgs(bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
		{
			this.changeAffectsFilterResult = changeAffectsFilterResult;
			this.changeAffectsPreprocessingResult = changeAffectsPreprocessingResult;
		}
		public bool ChangeAffectsFilterResult
		{
			get { return changeAffectsFilterResult; }
		}
		public bool ChangeAffectsPreprocessingResult { get { return changeAffectsPreprocessingResult; } }

		bool changeAffectsFilterResult;
		bool changeAffectsPreprocessingResult;
	};

	public class TooManyFiltersException : Exception
	{
	};

	public interface IFilter : IDisposable
	{
		IFiltersList Owner { get; }
		IFiltersFactory Factory { get; }
		bool IsDisposed { get; }
		FilterAction Action { get; set; }
		string Name { get; }
		string InitialName { get; }
		void SetUserDefinedName(string value);
		bool Enabled { get; set; }
		string Template { get; set; }
		bool WholeWord { get; set; }
		bool Regexp { get; set; }
		bool MatchCase { get; set; }
		MessageBase.MessageFlag Types { get; set; }
		bool MatchFrameContent { get; set; }
		IFilterTarget Target { get; set; }
		int Counter { get; }
		bool Match(MessageBase message, bool matchRawMessages);
		IFilter Clone(string newFilterInitialName);

		void SetOwner(IFiltersList newOwner);
		void IncrementCounter();
		void ResetCounter();
	};

	public interface IFilterTarget
	{
		bool MatchesAllSources { get; }
		bool MatchesSource(ILogSource src);
		bool MatchesThread(IThread thread);
		bool Match(MessageBase msg);
		IList<ILogSource> Sources { get; }
		IList<IThread> Threads { get; }
		bool IsDead { get; }
	};

	public interface IFiltersFactory
	{
		IFilterTarget CreateFilterTarget();
		IFilterTarget CreateFilterTarget(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads);

		IFilter CreateFilter(FilterAction type, string initialName, bool enabled, string template, bool wholeWord, bool regExp, bool matchCase);

		IFiltersList CreateFiltersList(FilterAction actionWhenEmptyOrDisabled);
	};
}
