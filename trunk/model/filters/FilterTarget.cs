using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LogJoint
{
	internal class FilterScope : IFilterScope
	{
		public FilterScope()
		{
		}

		public FilterScope(IEnumerable<ILogSource> includeAllFromSources, IEnumerable<IThread> includeAllFromThreads)
		{
			if (includeAllFromSources == null)
				throw new ArgumentNullException(nameof(includeAllFromSources));
			if (includeAllFromThreads == null)
				throw new ArgumentNullException(nameof(includeAllFromThreads));

			this.includeAllFromSources = new HashSet<ILogSource>(includeAllFromSources);
			this.includeAllFromThreads = new HashSet<IThread>(includeAllFromThreads);
			this.includeAnythingFromSources = new HashSet<ILogSource>(includeAllFromSources);
			this.includeAnythingFromSources.UnionWith(includeAllFromThreads.Select(t => t.LogSource));
		}

		bool IFilterScope.ContainsEverything 
		{ 
			get { return MatchesAllSourcesInternal(); } 
		}

		bool IFilterScope.ContainsEverythingFromSource(ILogSource src)
		{
			return MatchesSourceInternal(src);
		}

		bool IFilterScope.ContainsAnythingFromSource(ILogSource src)
		{
			return MatchesAllSourcesInternal() || includeAnythingFromSources.Contains(src);
		}

		bool IFilterScope.ContainsEverythingFromThread(IThread thread)
		{
			return MatchesThreadInternal(thread);
		}

		bool IFilterScope.ContainsMessage(IMessage msg)
		{
			return MatchesAllSourcesInternal() || MatchesSourceInternal(msg.Thread.LogSource) || MatchesThreadInternal(msg.Thread);
		}

		bool IFilterScope.IsDead
		{
			get
			{
				if (MatchesAllSourcesInternal())
					return false;
				if (includeAllFromSources != null && includeAllFromSources.Any(logSource => !logSource.IsDisposed))
					return false;
				if (includeAllFromThreads != null && includeAllFromThreads.Any(thread => !thread.IsDisposed))
					return false;
				return true;
			}
		}

		public override int GetHashCode()
		{
			if (hash == null)
				if (MatchesAllSourcesInternal())
					hash = RuntimeHelpers.GetHashCode(FiltersFactory.DefaultScope);
				else
					hash = Hashing.GetHashCode(includeAllFromSources.Select(s => s.GetHashCode()).Union(includeAllFromThreads.Select(t => t.GetHashCode())));
			return hash.Value;
		}

		public override bool Equals(object obj)
		{
			var other = obj as FilterScope;
			if (other == null)
				return false;
			if (MatchesAllSourcesInternal() != other.MatchesAllSourcesInternal())
				return false;
			if (MatchesAllSourcesInternal())
				return true;
			return
				includeAllFromSources.SetEquals(other.includeAllFromSources) &&
				includeAllFromThreads.SetEquals(other.includeAllFromThreads);
		}


		bool MatchesAllSourcesInternal()
		{
			return includeAllFromSources == null;
		}

		bool MatchesSourceInternal(ILogSource src)
		{
			if (MatchesAllSourcesInternal())
				throw new InvalidOperationException("This target matches all sources. Checking for single source is not allowed.");
			return includeAllFromSources.Contains(src);
		}

		bool MatchesThreadInternal(IThread thread)
		{
			if (MatchesAllSourcesInternal())
				throw new InvalidOperationException("This target matches all sources. Checking for single thread is not allowed.");
			return includeAllFromThreads.Contains(thread);
		}


		private readonly HashSet<ILogSource> includeAllFromSources;
		private readonly HashSet<IThread> includeAllFromThreads;
		private readonly HashSet<ILogSource> includeAnythingFromSources;
		private int? hash;
	};
}
