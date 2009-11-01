using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public interface IFiltersEvents
	{
		void OnFiltersListChanged();
		void OnPropertiesChanged(Filter f, bool changeAffectsFilterResult);
	};

	public enum FilterAction
	{
		Show = 0,
		Hide = 1,
	};

	public class FilterTarget
	{
		public FilterTarget()
		{
		}

		public FilterTarget(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads)
		{
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

		private readonly Dictionary<ILogSource, bool> sources;
		private readonly Dictionary<IThread, bool> threads;
	};

	public class Filter
	{
		public EventHandler Changed;

		public FilterAction Action
		{
			get	
			{ 
				return action; 
			}
			set 
			{
				if (action == value)
					return;
				action = value; 
				OnChange(true); 
				InvalidateDefaultAction(); 
			}
		}
		public string Name
		{
			get 
			{ 
				return name; 
			}
			set 
			{
				if (name == value)
					return;
				name = value; 
				OnChange(false); 
			}
		}
		public bool Enabled
		{
			get 
			{ 
				return enabled; 
			}
			set 
			{
				if (enabled == value)
					return;
				enabled = value; 
				OnChange(true); 
				InvalidateDefaultAction(); 
			}
		}

		public string Template
		{
			get 
			{ 
				return template; 
			}
			set 
			{
				if (template == value)
					return;
				template = value; 
				InternalInvalidate(); 
				OnChange(true); 
			}		
		}
		public bool WholeWord
		{
			get 
			{ 
				return wholeWord; 
			}
			set 
			{
				if (wholeWord == value)
					return;
				wholeWord = value; 
				OnChange(true); 
			}		
		}
		public bool Regexp
		{
			get 
			{ 
				return regexp; 
			}
			set 
			{
				if (regexp == value)
					return;
				regexp = value; 
				InternalInvalidate(); 
				OnChange(true); 
			}		
		}
		public bool MatchCase
		{
			get 
			{ 
				return matchCase; 
			}
			set 
			{
				if (matchCase == value)
					return;
				matchCase = value; 
				InternalInvalidate(); 
				OnChange(true); 
			}
		}
		public MessageBase.MessageFlag Types
		{
			get 
			{ 
				return typesToApplyFilterTo; 
			}
			set
			{
				if (value == typesToApplyFilterTo)
					return;
				typesToApplyFilterTo = value;
				OnChange(true); 
			}
		}

		public bool MatchFrameContent
		{
			get
			{
				return matchFrameContent;
			}
			set
			{
				if (value == matchFrameContent)
					return;
				matchFrameContent = value;
				OnChange(true);
			}
		}

		public FilterTarget Target
		{
			get 
			{ 
				return target; 
			}
			set 
			{
				if (value == null)
					throw new ArgumentNullException();
				target = value; 
				OnChange(true); 
			}
		}

		public FiltersList Owner { get { return owner; } }

		public Filter(FilterAction type, string name, bool enabled, string template, bool wholeWord, bool regExp, bool matchCase)
		{
			this.name = name;
			this.enabled = enabled;
			this.action = type;
			this.template = template;
			this.wholeWord = wholeWord;
			this.regexp = regExp;
			this.matchCase = matchCase;

			InternalInvalidate();
		}

		public bool Match(MessageBase message)
		{
			InternalUpdate();

			if (!MatchText(message))
				return false;

			if (!target.Match(message))
				return false;

			if (!MatchTypes(message))
				return false;

			return true;
		}

		public int Counter
		{
			get { return counter; }
		}

		public static bool IsWholeWord(string text, int matchBegin, int matchEnd)
		{
			if (matchBegin > 0)
				if (char.IsLetterOrDigit(text, matchBegin - 1))
					return false;
			if (matchEnd < text.Length - 1)
				if (char.IsLetterOrDigit(text, matchEnd))
					return false;
			return true;
		}

		bool MatchTypes(MessageBase msg)
		{
			MessageBase.MessageFlag typeAndContentType = msg.Flags & (MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask);
			return (typeAndContentType & typesToApplyFilterTo) == typeAndContentType;
		}

		bool MatchText(MessageBase msg)
		{
			if (string.IsNullOrEmpty(template))
				return true;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of the char following after the last matched one

			string text = msg.Text;

			int textPos = 0;
			if (this.re != null)
			{
				Match m = this.re.Match(text, textPos);
				if (!m.Success)
					return false;
				matchBegin = m.Index;
				matchEnd = matchBegin + m.Length;
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

		void InternalInvalidate()
		{
			this.invalidated = true;
			this.re = null;
		}

		void InvalidateDefaultAction()
		{
			if (owner != null)
				owner.InvalidateDefaultAction();
		}

		void InternalUpdate()
		{
			if (!invalidated)
				return;
			if (regexp)
			{
				RegexOptions reOpts = RegexOptions.None;
				if (matchCase)
					reOpts |= RegexOptions.IgnoreCase;
				re = new Regex(template, reOpts);
			}
			invalidated = false;
		}

		void OnChange(bool changeAffectsFilterResult)
		{
			if (owner != null)
				owner.events.OnPropertiesChanged(this, changeAffectsFilterResult);
		}

		internal FiltersList owner;

		private FilterAction action;
		private string name;
		private bool enabled;

		private string template;
		private bool wholeWord;
		private bool regexp;
		private bool matchCase;

		private bool invalidated;
		private Regex re;

		private FilterTarget target = FilterTarget.Default;
		internal int counter;
		private MessageBase.MessageFlag typesToApplyFilterTo = MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask;
		private bool matchFrameContent = true;
	};

	public class FiltersList
	{
		public FiltersList(IFiltersEvents events)
		{
			this.events = events;
		}

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
			list.Insert(position, filter);
			filter.owner = this;
			OnChanged();
		}
		public void RemoveAt(int position)
		{
			list[position].owner = null;
			list.RemoveAt(position);
			OnChanged();
		}
		public void Move(Filter f, bool upward)
		{
			int idx = -1;
			if (f.owner == this)
				idx = list.IndexOf(f);
			if (idx < 0)
				throw new ArgumentException("Filter doesn't belong to this list");

			if (upward)
			{
				if (idx > 0)
					Swap(idx, idx - 1);
			}
			else
			{
				if (idx < list.Count - 1)
					Swap(idx, idx + 1);
			}
			
			OnChanged();
		}
		public void Remove(IEnumerable<Filter> range)
		{
			int toRemove = 0;
			foreach (Filter f in range)
			{
				if (f.owner != this)
					throw new InvalidOperationException("Can not remove the filter that doesn't belong to the list");
				++toRemove;
			}
			if (toRemove == 0)
				return;

			foreach (Filter f in range)
			{
				list.Remove(f);
				f.owner = null;
			}

			OnChanged();
		}
		public void ResetFiltersCounters()
		{
			foreach (Filter f in list)
				f.counter = 0;
			defaultActionCounter = 0;
		}
		public FilterAction ProcessNextMessageAndGetItsAction(MessageBase msg)
		{
			IThread thread = msg.Thread;
			Filter regionFilter = thread.RegionFilter;
			if (regionFilter == null)
			{
				for (int i = 0; i < list.Count; ++i)
				{
					Filter f = list[i];
					if (f.Enabled && f.Match(msg))
					{
						f.counter++;
						if (f.MatchFrameContent && (msg.Flags & MessageBase.MessageFlag.StartFrame) != 0)
						{
							thread.BeginFilterRegion(f);
						}
						return f.Action;
					}
				}
			}
			else
			{
				if ((msg.Flags & MessageBase.MessageFlag.EndFrame) != 0)
				{
					thread.EndFilterRegion();
				}
				regionFilter.counter++;
				return regionFilter.Action;
			}

			defaultActionCounter++;
			return GetDefaultAction();
		}
		public FilterAction GetDefaultAction()
		{
			if (!defaultAction.HasValue)
			{
				if (list.Count > 0)
				{
					Filter last = list[list.Count - 1];
					if (!last.Enabled && list.Count == 1)
					{
						defaultAction = FilterAction.Show;
					}
					else
					{
						defaultAction = last.Action == FilterAction.Hide ? FilterAction.Show : FilterAction.Hide;
					}
				}
				else
				{
					defaultAction = FilterAction.Show;
				}
			}
			return defaultAction.Value;
		}
		public int GetDefaultActionCounter() 
		{ 
			return defaultActionCounter; 
		}

		private void OnChanged()
		{
			InvalidateDefaultAction();
			if (events == null)
				return;
			events.OnFiltersListChanged();
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

		internal readonly IFiltersEvents events;
		readonly List<Filter> list = new List<Filter>();
		FilterAction? defaultAction;
		int defaultActionCounter;
	}
}
