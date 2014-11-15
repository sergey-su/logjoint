using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	internal class FilterTarget : IFilterTarget
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

		bool IFilterTarget.MatchesAllSources 
		{ 
			get { return MatchesAllSourcesInternal(); } 
		}

		bool IFilterTarget.MatchesSource(ILogSource src)
		{
			return MatchesSourceInternal(src);
		}

		bool IFilterTarget.MatchesThread(IThread thread)
		{
			return MatchesThreadInternal(thread);
		}

		bool IFilterTarget.Match(MessageBase msg)
		{
			return MatchesAllSourcesInternal() || MatchesSourceInternal(msg.Thread.LogSource) || MatchesThreadInternal(msg.Thread);
		}

		IList<ILogSource> IFilterTarget.Sources
		{
			get { return new List<ILogSource>(sources.Keys); }
		}

		IList<IThread> IFilterTarget.Threads
		{
			get { return new List<IThread>(threads.Keys); }
		}

		bool IFilterTarget.IsDead
		{
			get
			{
				if (MatchesAllSourcesInternal())
					return false;
				if (sources != null && sources.Keys.Any(logSource => !logSource.IsDisposed))
					return false;
				if (threads != null && threads.Keys.Any(thread => !thread.IsDisposed))
					return false;
				return true;
			}
		}

		bool MatchesAllSourcesInternal()
		{
			return sources == null;
		}

		bool MatchesSourceInternal(ILogSource src)
		{
			if (MatchesAllSourcesInternal())
				throw new InvalidOperationException("This target matches all sources. Checking for single source is not allowed.");
			return sources.ContainsKey(src);
		}

		bool MatchesThreadInternal(IThread thread)
		{
			if (MatchesAllSourcesInternal())
				throw new InvalidOperationException("This target matches all sources. Checking for single thread is not allowed.");
			return threads.ContainsKey(thread);
		}


		private readonly Dictionary<ILogSource, bool> sources;
		private readonly Dictionary<IThread, bool> threads;
	};
}
