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

	public class FilterTarget
	{
		public FilterTarget()
		{
		}

		public FilterTarget(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads)
		{
			if (sources == null)
				throw new ArgumentNullException("sources");
			if (threads == null)
				throw new ArgumentNullException("threads");

			this.sources = new Dictionary<ILogSource, bool>();
			this.threads = new Dictionary<IThread, bool>();

			foreach (ILogSource s in sources)
				this.sources[s] = true;
			foreach (IThread t in threads)
				this.threads[t] = true;
		}

		public static readonly FilterTarget Default = new FilterTarget();

		public bool MatchesAllSources 
		{ 
			get { return sources == null; } 
		}
		public bool MatchesSource(ILogSource src)
		{
			if (MatchesAllSources)
				throw new InvalidOperationException("This target matches all sources. Checking for single source is not allowed.");
			return sources.ContainsKey(src);
		}
		public bool MatchesThread(IThread thread)
		{
			if (MatchesAllSources)
				throw new InvalidOperationException("This target matches all sources. Checking for single thread is not allowed.");
			return threads.ContainsKey(thread);
		}

		public bool Match(MessageBase msg)
		{
			return MatchesAllSources || MatchesSource(msg.Thread.LogSource) || MatchesThread(msg.Thread);
		}

		public IList<ILogSource> Sources
		{
			get { return new List<ILogSource>(sources.Keys); }
		}
		public IList<IThread> Threads
		{
			get { return new List<IThread>(threads.Keys); }
		}

		public bool IsDead
		{
			get
			{
				if (MatchesAllSources)
					return false;
				if (sources != null && sources.Keys.Any(logSource => !logSource.IsDisposed))
					return false;
				if (threads != null && threads.Keys.Any(thread => !thread.IsDisposed))
					return false;
				return true;
			}
		}

		private readonly Dictionary<ILogSource, bool> sources;
		private readonly Dictionary<IThread, bool> threads;
	};

	public class Filter: IDisposable
	{
		public EventHandler Changed;

		public FilterAction Action
		{
			get
			{
				CheckDisposed();
				return action; 
			}
			set 
			{
				CheckDisposed();
				if (action == value)
					return;
				action = value; 
				OnChange(true, false); 
				InvalidateDefaultAction(); 
			}
		}
		public string Name
		{
			get 
			{
				CheckDisposed();
				InternalInsureName();
				return name; 
			}
		}
		public string InitialName { get { return initialName; } }
		public void SetUserDefinedName(string value)
		{
			CheckDisposed();
			InternalInsureName();
			if (name == value)
				return;
			if (string.IsNullOrEmpty(value))
				value = null;
			userDefinedName = value;
			InvalidateName();
			OnChange(false, false);
		}
		public bool Enabled
		{
			get 
			{
				CheckDisposed();
				return enabled; 
			}
			set 
			{
				CheckDisposed();
				if (enabled == value)
					return;
				enabled = value; 
				OnChange(true, false); 
				InvalidateDefaultAction();
			}
		}


		public string Template
		{
			get 
			{
				CheckDisposed();
				return template; 
			}
			set 
			{
				CheckDisposed();
				if (template == value)
					return;
				template = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		public bool WholeWord
		{
			get 
			{
				CheckDisposed();
				return wholeWord; 
			}
			set 
			{
				CheckDisposed();
				if (wholeWord == value)
					return;
				wholeWord = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}
		public bool Regexp
		{
			get 
			{
				CheckDisposed();
				return regexp; 
			}
			set 
			{
				CheckDisposed();
				if (regexp == value)
					return;
				regexp = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		public bool MatchCase
		{
			get 
			{
				CheckDisposed();
				return matchCase; 
			}
			set 
			{
				CheckDisposed();
				if (matchCase == value)
					return;
				matchCase = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		public MessageBase.MessageFlag Types
		{
			get 
			{
				CheckDisposed();
				return typesToApplyFilterTo; 
			}
			set
			{
				CheckDisposed();
				if (value == typesToApplyFilterTo)
					return;
				typesToApplyFilterTo = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}

		public bool MatchFrameContent
		{
			get
			{
				CheckDisposed();
				return matchFrameContent;
			}
			set
			{
				CheckDisposed();
				if (value == matchFrameContent)
					return;
				matchFrameContent = value;
				InvalidateName();
				OnChange(true, false);
			}
		}

		public FilterTarget Target
		{
			get 
			{
				CheckDisposed();
				return target; 
			}
			set 
			{
				CheckDisposed();
				if (value == null)
					throw new ArgumentNullException();
				target = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}

		public FiltersList Owner { get { return owner; } }

		public Filter(FilterAction type, string initialName, bool enabled, string template, bool wholeWord, bool regExp, bool matchCase)
		{
			if (initialName == null)
				throw new ArgumentNullException("initialName");

			this.initialName = initialName;
			this.enabled = enabled;
			this.action = type;
			this.template = template;
			this.wholeWord = wholeWord;
			this.regexp = regExp;
			this.matchCase = matchCase;

			InvalidateRegex();
			InvalidateName();
		}

		public Filter Clone(string newFilterInitialName)
		{
			var ret = new Filter(action, newFilterInitialName, enabled, template, wholeWord, regexp, matchCase);
			ret.Target = target; // FilterTarget is readonly. Safe to refer to the same object.
			ret.Types = typesToApplyFilterTo;
			ret.MatchFrameContent = matchFrameContent;
			return ret;
		}

		public virtual void Dispose()
		{
			if (isDisposed)
				return;
			owner = null;
			isDisposed = true;
		}

		public bool IsDisposed
		{
			get { return isDisposed; }
		}

		protected void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(this.ToString());
		}

		public bool Match(MessageBase message, bool matchRawMessages)
		{
			CheckDisposed();
			InternalInsureRegex();

			if (!MatchText(message, matchRawMessages))
				return false;

			if (!target.Match(message))
				return false;

			if (!MatchTypes(message))
				return false;

			return true;
		}

		public int Counter
		{
			get { CheckDisposed(); return counter; }
		}

		public static bool IsWholeWord(StringSlice text, int matchBegin, int matchEnd)
		{
			if (matchBegin > 0)
				if (StringUtils.IsWordChar(text[matchBegin - 1]))
					return false;
			if (matchEnd < text.Length - 1)
				if (StringUtils.IsWordChar(text[matchEnd]))
					return false;
			return true;
		}

		#region Implementation

		internal virtual void SetOwner(FiltersList newOwner)
		{
			CheckDisposed();
			if (newOwner != null && owner != null)
				throw new InvalidOperationException("Filter can not be attached to FiltersList: already attached to another list");
			owner = newOwner;
		}

		bool MatchTypes(MessageBase msg)
		{
			MessageBase.MessageFlag typeAndContentType = msg.Flags & (MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask);
			return (typeAndContentType & typesToApplyFilterTo) == typeAndContentType;
		}

		bool MatchText(MessageBase msg, bool matchRawMessages)
		{
			if (string.IsNullOrEmpty(template))
				return true;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of the char following after the last matched one

			StringSlice text = matchRawMessages ? msg.RawText : msg.Text;

			int textPos = 0;
			if (this.re != null)
			{
				if (!this.re.Match(text, textPos, ref reMatch))
					return false;
				matchBegin = reMatch.Index;
				matchEnd = matchBegin + reMatch.Length;
			}
			else
			{
				int i = text.IndexOf(this.template, textPos, 
					this.matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					return false;
				matchBegin = i;
				matchEnd = matchBegin + this.template.Length;
			}

			if (this.WholeWord)
			{
				if (!IsWholeWord(text, matchBegin, matchEnd))
					return false;
			}

			return true;
		}

		void InvalidateRegex()
		{
			this.regexInvalidated = true;
			this.re = null;
			this.reMatch = null;
		}

		void InvalidateName()
		{
			this.nameInvalidated = true;
			this.name = null;
		}

		void InvalidateDefaultAction()
		{
			if (owner != null)
				owner.InvalidateDefaultAction();
		}

		void InternalUpdateRegex()
		{
			if (regexp)
			{
				ReOptions reOpts = ReOptions.None;
				if (!matchCase)
					reOpts |= ReOptions.IgnoreCase;
				re = RegexFactory.Instance.Create(template, reOpts);
				reMatch = null;
			}
		}

		void InternalUpdateName()
		{
			if (userDefinedName != null)
			{
				name = userDefinedName;
				return;
			}
			List<string> templateIndependentModifiers = new List<string>();
			GetTemplateIndependentModifiers(templateIndependentModifiers);
			if (!string.IsNullOrEmpty(template))
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(template);
				List<string> modifiers = new List<string>();
				GetTemplateDependentModifiers(modifiers);
				modifiers.AddRange(templateIndependentModifiers);
				ConcatModifiers(builder, modifiers);
				name = builder.ToString();
			}
			else if (templateIndependentModifiers.Count > 0)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("<any text>");
				ConcatModifiers(builder, templateIndependentModifiers);
				name = builder.ToString();
			}
			else
			{
				name = initialName;
			}
		}

		static void ConcatModifiers(StringBuilder ret, List<string> modifiers)
		{
			if (modifiers.Count > 0)
			{
				ret.Append(" (");
				for (int i = 0; i < modifiers.Count; ++i)
				{
					if (i > 0)
						ret.Append(", ");
					ret.Append(modifiers[i]);
				}
				ret.Append(")");
			}
		}

		void GetTemplateDependentModifiers(List<string> modifiers)
		{
			if (matchCase)
				modifiers.Add("match case");
			if (wholeWord)
				modifiers.Add("whole word");
			if (regexp)
				modifiers.Add("regexp");
		}

		void GetTemplateIndependentModifiers(List<string> modifiers)
		{
			if (this.typesToApplyFilterTo == 0)
			{
				modifiers.Add("no types to match!");
				return;
			}
			MessageBase.MessageFlag contentTypes = this.typesToApplyFilterTo & MessageBase.MessageFlag.ContentTypeMask;
			if (contentTypes != MessageBase.MessageFlag.ContentTypeMask)
			{
				if ((contentTypes & MessageBase.MessageFlag.Info) != 0)
					modifiers.Add("infos");
				if ((contentTypes & MessageBase.MessageFlag.Warning) != 0)
					modifiers.Add("warns");
				if ((contentTypes & MessageBase.MessageFlag.Error) != 0)
					modifiers.Add("errs");
			}
			MessageBase.MessageFlag types = this.typesToApplyFilterTo & MessageBase.MessageFlag.TypeMask;
			if (types != MessageBase.MessageFlag.TypeMask)
			{
				if ((types & MessageBase.MessageFlag.StartFrame) == 0 && (types & MessageBase.MessageFlag.EndFrame) == 0)
					modifiers.Add("no frames");
			}
		}

		void InternalInsureRegex()
		{
			CheckDisposed();
			if (!regexInvalidated)
				return;
			InternalUpdateRegex();
			regexInvalidated = false;
		}

		void InternalInsureName()
		{
			CheckDisposed();
			if (!nameInvalidated)
				return;
			InternalUpdateName();
			nameInvalidated = false;
		}

		protected void OnChange(bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
		{
			if (owner != null)
				owner.FireOnPropertiesChanged(this, changeAffectsFilterResult, changeAffectsPreprocessingResult);
		}

		#endregion

		#region Members

		private bool isDisposed;
		private FiltersList owner;
		private readonly string initialName;
		private string userDefinedName;

		private FilterAction action;
		private bool enabled;

		private string template;
		private bool wholeWord;
		private bool regexp;
		private bool matchCase;

		private bool regexInvalidated;
		private IRegex re;
		private IMatch reMatch;
		private bool nameInvalidated;
		private string name;

		private FilterTarget target = FilterTarget.Default;
		internal int counter;
		private MessageBase.MessageFlag typesToApplyFilterTo = MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask;
		private bool matchFrameContent = true;

		#endregion
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

	public class FiltersList: IDisposable
	{
		public FiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			this.actionWhenEmptyOrDisabled = actionWhenEmptyOrDisabled;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			foreach (Filter f in list)
			{
				f.Dispose();
			}
			list.Clear();
			OnFiltersListChanged = null;
			OnPropertiesChanged = null;
			OnCountersChanged = null;
		}

		public FiltersList Clone()
		{
			FiltersList ret = new FiltersList(actionWhenEmptyOrDisabled);
			ret.FilteringEnabled = filteringEnabled;
			foreach (var f in Items)
				ret.Insert(ret.Count, f.Clone(f.InitialName));
			return ret;
		}

		public static FiltersList EmptyAndDisabled
		{
			get { return emptyAndDisabled; }
		}

		public bool FilteringEnabled
		{
			get
			{
				return filteringEnabled;
			}
			set
			{
				if (value == filteringEnabled)
					return;
				filteringEnabled = value;
				OnFilteringEnabledOrDisabled();
			}
		}

		#region Events
		public event EventHandler OnFiltersListChanged;
		public event EventHandler OnFilteringEnabledChanged;
		public event EventHandler<FilterChangeEventArgs> OnPropertiesChanged;
		public event EventHandler OnCountersChanged;
		#endregion

		#region Filters access and manipulation
		public IEnumerable<Filter> Items
		{
			get { return list; }
		}
		public int Count
		{
			get { return list.Count; }
		}
		public void Insert(int position, Filter filter)
		{
			if (filter == null)
				throw new ArgumentNullException("filter");
			if (list.Count == PreprocessingResult.MaxEnabledFiltersSupportedByPreprocessing)
				throw new TooManyFiltersException();
			filter.SetOwner(this);
			list.Insert(position, filter);
			OnChanged();
		}
		public bool Move(Filter f, bool upward)
		{
			int idx = -1;
			if (f.Owner == this)
				idx = list.IndexOf(f);
			if (idx < 0)
				throw new ArgumentException("Filter doesn't belong to this list");

			bool movePossible;
			if (upward)
			{
				if ((movePossible = idx > 0) == true)
					Swap(idx, idx - 1);
			}
			else
			{
				if ((movePossible = idx < list.Count - 1) == true)
					Swap(idx, idx + 1);
			}
			
			if (movePossible)
				OnChanged();

			return movePossible;
		}
		public void Delete(IEnumerable<Filter> range)
		{
			int toRemove = 0;
			foreach (Filter f in range)
			{
				if (f.Owner != this)
					throw new InvalidOperationException("Can not remove the filter that doesn't belong to the list");
				++toRemove;
			}
			if (toRemove == 0)
				return;

			foreach (Filter f in range)
			{
				list.Remove(f);
				f.SetOwner(null);
				f.Dispose();
			}

			OnChanged();
		}
		public int PurgeDisposedFiltersAndFiltersHavingDisposedThreads()
		{
			int i = ListUtils.RemoveIf(list, 0, list.Count, f => f.IsDisposed || f.Target.IsDead);

			int itemsToRemove = list.Count - i;
			if (itemsToRemove == 0)
				return itemsToRemove;

			for (int j = i; j < list.Count; ++j)
			{
				if (!list[j].IsDisposed)
				{
					list[j].SetOwner(null);
					list[j].Dispose();
				}
			}
			list.RemoveRange(i, itemsToRemove);

			OnChanged();

			return itemsToRemove;
		}
		#endregion

		#region Bulk processing
		public class BulkProcessingHandle
		{
			internal int[] counters;
		};
		public BulkProcessingHandle BeginBulkProcessing()
		{
			BulkProcessingHandle ret = new BulkProcessingHandle();

			StoreCounters(ret);

			foreach (Filter f in list)
				f.counter = 0;
			defaultActionCounter = 0;
			
			return ret;
		}
		public void EndBulkProcessing(BulkProcessingHandle handle)
		{
			if (HaveCountersChanged(handle))
				FireOnCountersChanged();
		}
		#endregion

		#region Messages processing
		public struct PreprocessingResult
		{
			internal UInt64 mask;

			internal const int MaxEnabledFiltersSupportedByPreprocessing = 64;
		};

		public PreprocessingResult PreprocessMessage(MessageBase msg, bool matchRawMessages)
		{
			UInt64 mask = 0;

			if (filteringEnabled)
			{
				UInt64 nextBitToSet = 1;
				for (int i = 0; i < list.Count; ++i)
				{
					Filter f = list[i];
					if (f.Match(msg, matchRawMessages))
						mask |= nextBitToSet;
					unchecked { nextBitToSet *= 2; }
				}
			}

			PreprocessingResult ret;
			ret.mask = mask;
			return ret;
		}

		public FilterAction ProcessNextMessageAndGetItsAction(MessageBase msg, PreprocessingResult preprocessingResult, FilterContext filterCtx, bool matchRawMessages)
		{
			return ProcessNextMessageAndGetItsActionImpl(msg, filterCtx, preprocessingResult.mask, true, matchRawMessages);
		}

		public FilterAction ProcessNextMessageAndGetItsAction(MessageBase msg, FilterContext filterCtx, bool matchRawMessages)
		{
			return ProcessNextMessageAndGetItsActionImpl(msg, filterCtx, 0, false, matchRawMessages);
		}

		public FilterAction GetDefaultAction()
		{
			if (!defaultAction.HasValue)
			{
				if (list.Count > 0)
				{
					defaultAction = actionWhenEmptyOrDisabled;
					for (int i = list.Count - 1; i >= 0; --i)
					{
						var f = list[i];
						if (!f.IsDisposed && f.Enabled)
						{
							defaultAction = f.Action == FilterAction.Exclude ? FilterAction.Include : FilterAction.Exclude;
							break;
						}
					}
				}
				else
				{
					defaultAction = actionWhenEmptyOrDisabled;
				}
			}
			return defaultAction.Value;
		}
		public int GetDefaultActionCounter() 
		{ 
			return defaultActionCounter;
		}
		#endregion

		#region Implementation

		static FiltersList()
		{
			emptyAndDisabled = new FiltersList(FilterAction.Include);
			emptyAndDisabled.FilteringEnabled = false;
		}

		private void OnChanged()
		{
			InvalidateDefaultAction();
			if (OnFiltersListChanged != null)
				OnFiltersListChanged(this, EventArgs.Empty);
		}

		private void OnFilteringEnabledOrDisabled()
		{
			InvalidateDefaultAction();
			if (OnFilteringEnabledChanged != null)
				OnFilteringEnabledChanged(this, EventArgs.Empty);
		}

		internal void InvalidateDefaultAction()
		{
			defaultAction = null;
		}

		void Swap(int idx1, int idx2)
		{
			Filter tmp = list[idx1];
			list[idx1] = list[idx2];
			list[idx2] = tmp;
		}

		internal void FireOnPropertiesChanged(Filter sender, bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
		{
			if (OnPropertiesChanged != null)
				OnPropertiesChanged(sender, new FilterChangeEventArgs(changeAffectsFilterResult, changeAffectsPreprocessingResult));
		}

		internal void FireOnCountersChanged()
		{
			if (OnCountersChanged != null)
				OnCountersChanged(this, EventArgs.Empty);
		}

		void StoreCounters(BulkProcessingHandle handle)
		{
			handle.counters = new int[list.Count + 1];
			int idx = 0;
			foreach (Filter f in list)
			{
				handle.counters[idx++] = f.counter;
			}
			handle.counters[idx++] = defaultActionCounter;
		}

		bool HaveCountersChanged(BulkProcessingHandle handle)
		{
			var savedCounters = handle.counters;

			if (savedCounters.Length != list.Count + 1)
				return true;

			int idx = 0;
			foreach (Filter f in list)
			{
				if (savedCounters[idx++] != f.counter)
					return true;
			}
			if (savedCounters[idx++] != defaultActionCounter)
				return true;

			return false;
		}

		FilterAction ProcessNextMessageAndGetItsActionImpl(MessageBase msg, FilterContext filterCtx, UInt64 mask, bool maskValid, bool matchRawMessages)
		{
			if (!filteringEnabled)
			{
				return actionWhenEmptyOrDisabled;
			}
			IThread thread = msg.Thread;
			Filter regionFilter = filterCtx.RegionFilter;
			if (regionFilter == null)
			{
				UInt64 nextMaskBitToCheck = 1;
				for (int i = 0; i < list.Count; ++i)
				{
					Filter f = list[i];
					if (f.Enabled)
					{
						bool match;
						if (maskValid)
							match = (mask & nextMaskBitToCheck) != 0;
						else
							match = f.Match(msg, matchRawMessages);
						if (match)
						{
							f.counter++;
							if (f.MatchFrameContent && (msg.Flags & MessageBase.MessageFlag.StartFrame) != 0)
							{
								filterCtx.BeginRegion(f);
							}
							return f.Action;
						}
					}
					if (maskValid)
					{
						unchecked { nextMaskBitToCheck *= 2; }
					}
				}
			}
			else
			{
				switch (msg.Flags & MessageBase.MessageFlag.TypeMask)
				{
					case MessageBase.MessageFlag.StartFrame:
						filterCtx.BeginRegion(regionFilter);
						break;
					case MessageBase.MessageFlag.EndFrame:
						filterCtx.EndRegion();
						break;
				}
				regionFilter.counter++;
				return regionFilter.Action;
			}

			defaultActionCounter++;
			return GetDefaultAction();
		}

		#endregion

		#region Members

		bool disposed;
		readonly List<Filter> list = new List<Filter>();
		readonly FilterAction actionWhenEmptyOrDisabled;
		FilterAction? defaultAction;
		int defaultActionCounter;
		bool filteringEnabled = true;
		static FiltersList emptyAndDisabled;

		#endregion
	}
}
