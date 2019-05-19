using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace LogJoint
{
	public enum FilterAction
	{
		Exclude = 0,
		Include = 1,
		IncludeAndColorize1,
		IncludeAndColorize2,
		IncludeAndColorize3,
		IncludeAndColorize4,
		IncludeAndColorize5,
		IncludeAndColorize6,
		IncludeAndColorize7,
		IncludeAndColorize8,
		IncludeAndColorize9,
		IncludeAndColorize11,
		IncludeAndColorize12,
		IncludeAndColorize13,
		IncludeAndColorize14,
		IncludeAndColorize15,
		IncludeAndColorize16,
		IncludeAndColorize17,
		IncludeAndColorizeFirst = IncludeAndColorize1,
		IncludeAndColorizeLast = IncludeAndColorize17,
	};

	public struct MessageFilteringResult
	{
		public FilterAction Action;
		public IFilter Filter;
		public Search.MatchedTextRange? MatchedRange;
	};

	public interface IFiltersListBulkProcessing: IDisposable
	{
		MessageFilteringResult ProcessMessage(IMessage msg, int? startFromChar);
	}; 

	public interface IFiltersList : IDisposable
	{
		int PurgeDisposedFiltersAndFiltersHavingDisposedThreads();

		/// <summary>
		/// Creates an object that can be used to efficiently test many messages against this filters list.
		/// Returned object is a readonly snapshot of current filters list. It does not reflect any changes made to 
		/// the filters list after snapshot is created.
		/// Returned object can not be shared between different threads. Each thread has to call this method.
		/// </summary>
		IFiltersListBulkProcessing StartBulkProcessing(
			MessageTextGetter messageTextGetter, bool reverseMatchDirection, bool timeboxedMatching = false);
		
		IFiltersList Clone();
		FiltersListPurpose Purpose { get; }
		bool FilteringEnabled { get; set; }
		void Insert(int position, IFilter filter);
		void Delete(IEnumerable<IFilter> range);
		bool Move(IFilter f, bool upward);
		ImmutableList<IFilter> Items { get; }
		FilterAction GetDefaultAction();
		int FiltersVersion { get; }
		void Save(XElement e);

		event EventHandler OnFiltersListChanged;
		event EventHandler OnFilteringEnabledChanged;
		event EventHandler<FilterChangeEventArgs> OnPropertiesChanged;

		void InvalidateDefaultAction();
		void FireOnPropertiesChanged(IFilter sender, bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult);
	};

	public enum FiltersListPurpose
	{
		None,
		Highlighting,
		Search,
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

	public interface IFilterBulkProcessing : IDisposable
	{
		Search.MatchedTextRange? Match(IMessage message, int? startFromChar);
	};

	/// <summary>
	/// Filter object can match log message by text template, or by
	/// message's severity, or by message's thread. Filters matching is used 
	/// to highlight messages or to include messages to search result. 
	/// Such objects are called "Rules" in UI.
	/// </summary>
	public interface IFilter : IDisposable
	{
		IFiltersList Owner { get; }
		IFilter Clone();
		IFiltersFactory Factory { get; }
		bool IsDisposed { get; }
		FilterAction Action { get; set; }
		string Name { get; }
		string InitialName { get; }
		string UserDefinedName { get; set; }
		bool Enabled { get; set; }
		Search.Options Options { get; set; }
		void Save(XElement e);

		IFilterBulkProcessing StartBulkProcessing(
			MessageTextGetter messageTextGetter, bool reverseMatchDirection, bool timeboxedMatching = false);

		void SetOwner(IFiltersList newOwner);
	};

	/// <summary>
	/// Immutable object that determines the scope of filter.
	/// The scope limits determines which log sources and which threads
	/// the filter is applicable to.
	/// </summary>
	public interface IFilterScope
	{
		bool ContainsEverything { get; }
		bool ContainsEverythingFromSource(ILogSource src);
		bool ContainsAnythingFromSource(ILogSource src);
		bool ContainsEverythingFromThread(IThread thread);
		bool ContainsMessage(IMessage msg);
		bool IsDead { get; }
		int GetHashCode();
		bool Equals(object scope);
	};

	public interface IFiltersFactory
	{
		IFilterScope CreateScope();
		IFilterScope CreateScope(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads);

		IFilter CreateFilter(FilterAction type, string initialName, bool enabled, Search.Options searchOptions);
		IFilter CreateFilter(XElement e);

		IFiltersList CreateFiltersList(FilterAction actionWhenEmptyOrDisabled, FiltersListPurpose purpose);
		IFiltersList CreateFiltersList(XElement e, FiltersListPurpose purpose);
	};
}
