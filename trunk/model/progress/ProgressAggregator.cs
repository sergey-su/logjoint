using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace LogJoint.Progress
{
	public class ProgressAggregator : IProgressAggregator
	{
		// readonly objects
		readonly IInvokeSynchronization invoker;
		readonly ProgressAggregator parent, root;
		readonly object sync = new object();

		// data below can be accessed from multiple user threads as well as from model thread.
		// access protected by sync
		readonly HashSet<ProgressEventsSink> sinks = new HashSet<ProgressEventsSink>();
		readonly HashSet<ProgressAggregator> children = new HashSet<ProgressAggregator>();
		int completedContributorsCount;

		// data below is accessed from model thread only
		bool isProgressActive;
		double? lastValue;

		public class Factory : IProgressAggregatorFactory
		{
			readonly IHeartBeatTimer timer;
			readonly IInvokeSynchronization invoker;

			public Factory(IHeartBeatTimer timer, IInvokeSynchronization invoker)
			{
				this.timer = timer;
				this.invoker = invoker;
			}

			IProgressAggregator IProgressAggregatorFactory.CreateProgressAggregator()
			{
				return new ProgressAggregator(timer, invoker);
			}
		};

		ProgressAggregator(IHeartBeatTimer timer, IInvokeSynchronization invoker)
		{
			this.invoker = invoker;
			this.root = this;
			timer.OnTimer += (s, e) =>
			{
				if (e.IsNormalUpdate)
					RootUpdate();
			};
		}

		ProgressAggregator(ProgressAggregator parent)
		{
			this.parent = parent;
			this.invoker = parent.invoker;
			this.root = parent.root;
			parent.Add(this);
		}

		IProgressEventsSink IProgressAggregator.CreateProgressSink()
		{
			return new ProgressEventsSink(this);
		}

		IProgressAggregator IProgressAggregator.CreateChildAggregator()
		{
			return new ProgressAggregator(this);
		}

		double? IProgressAggregator.ProgressValue { get { return lastValue; } }

		public event EventHandler<EventArgs> ProgressStarted;

		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

		public event EventHandler<EventArgs> ProgressEnded;

		void IDisposable.Dispose()
		{
			if (parent != null)
				parent.Remove(this);
		}


		void Add(ProgressEventsSink sink)
		{
			lock (sync)
				sinks.Add(sink);
		}

		void Remove(ProgressEventsSink sink)
		{
			lock (sync)
			{
				if (!sinks.Remove(sink))
					return;
				++completedContributorsCount;
			}
			invoker.Invoke(root.RootUpdate);
		}

		void Add(ProgressAggregator child)
		{
			lock (sync)
				children.Add(child);
		}

		void Remove(ProgressAggregator child)
		{
			lock (sync)
			{
				if (!children.Remove(child))
					return;
				++completedContributorsCount;
			}
			invoker.Invoke(root.RootUpdate);
		}

		AggUpdateInfo BeginUpdate()
		{
			bool active;
			double progress;
			List<AggUpdateInfo> childrenUpdates = null;
			lock (sync)
			{
				var activeContributorsCount = sinks.Count + children.Count;
				active = activeContributorsCount != 0;
				if (active)
				{
					progress = 0;
					if (sinks.Count > 0)
					{
						progress += sinks.Sum(s => s.Value);
					}
					if (children.Count > 0)
					{
						childrenUpdates = children.Select(c => c.BeginUpdate()).ToList();
						progress += childrenUpdates.Aggregate(0d, (x, upd) => x + upd.progress);
					}
					if (completedContributorsCount > 0)
					{
						progress += completedContributorsCount;
					}
					progress = progress / (double)(activeContributorsCount + completedContributorsCount);
				}
				else
				{
					if (parent == null)
						completedContributorsCount = 0;
					if (completedContributorsCount > 0)
						progress = 1;
					else
						progress = 0;
				}
			}
			return new AggUpdateInfo
			{
				active = active,
				progress = progress,
				childrenUpdates = childrenUpdates,
				agg = this
			};
		}

		void EndUpdate(AggUpdateInfo info)
		{
			if (info.childrenUpdates != null)
				info.childrenUpdates.ForEach(upd => upd.agg.EndUpdate(upd));
			bool active = info.active;
			double progress = info.progress;
			EventHandler<EventArgs> startStop = null;
			if (active != isProgressActive)
				startStop = active ? ProgressStarted : ProgressEnded;
			isProgressActive = active;
			lastValue = active ? progress : new double?();
			if (startStop != null)
				startStop(this, EventArgs.Empty);
			if (active && ProgressChanged != null)
				ProgressChanged(this, new ProgressChangedEventArgs((int)(progress * 100f), null));
		}

		void RootUpdate()
		{
			Debug.Assert(parent == null);
			EndUpdate(BeginUpdate());
		}

		class AggUpdateInfo
		{
			public ProgressAggregator agg;
			public bool active;
			public double progress;
			public List<AggUpdateInfo> childrenUpdates;
		};

		class ProgressEventsSink : IProgressEventsSink
		{
			readonly ProgressAggregator owner;
			double value;

			public ProgressEventsSink(ProgressAggregator owner)
			{
				this.owner = owner;
				owner.Add(this);
			}

			public double Value { get { return value; } }

			void IProgressEventsSink.SetValue(double value)
			{
				this.value = value;
			}

			void IDisposable.Dispose()
			{
				owner.Remove(this);
			}
		};
	}
}
