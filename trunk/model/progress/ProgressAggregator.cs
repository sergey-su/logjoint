using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.Progress
{
    public class ProgressAggregator : IProgressAggregator
    {
        // readonly objects
        readonly ISynchronizationContext invoker;
        readonly Func<TimeSpan, Task> sleep;
        readonly ProgressAggregator parent, root;
        readonly object sync = new object();

        // data below can be accessed from multiple user threads as well as from model thread.
        // access protected by sync
        readonly HashSet<ProgressEventsSink> sinks = new HashSet<ProgressEventsSink>();
        readonly HashSet<ProgressAggregator> children = new HashSet<ProgressAggregator>();
        int completedContributorsCount;
        Task periodic;
        long currentVersion = 0;

        // data below is accessed from model thread only
        bool isProgressActive;
        double? lastValue;

        public class Factory : IProgressAggregatorFactory
        {
            readonly ISynchronizationContext invoker;
            readonly Func<TimeSpan, Task> sleep;

            public Factory(ISynchronizationContext invoker, Func<TimeSpan, Task> sleep = null)
            {
                this.invoker = invoker;
                this.sleep = sleep ?? Task.Delay;
            }

            IProgressAggregator IProgressAggregatorFactory.CreateProgressAggregator()
            {
                return new ProgressAggregator(invoker, sleep);
            }
        };

        ProgressAggregator(ISynchronizationContext invoker, Func<TimeSpan, Task> sleep)
        {
            this.invoker = invoker;
            this.sleep = sleep;
            this.root = this;
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
            {
                sinks.Add(sink);
                UpdateRootPeriodic();
            }
        }

        void Remove(ProgressEventsSink sink)
        {
            lock (sync)
            {
                if (!sinks.Remove(sink))
                    return;
                ++completedContributorsCount;
                UpdateRootPeriodic();
            }
            invoker.Post(root.RootUpdate);
        }

        void Add(ProgressAggregator child)
        {
            lock (sync)
            {
                children.Add(child);
                UpdateRootPeriodic();
            }
        }

        void Remove(ProgressAggregator child)
        {
            lock (sync)
            {
                if (!children.Remove(child))
                    return;
                ++completedContributorsCount;
                UpdateRootPeriodic();
            }
            invoker.Post(root.RootUpdate);
        }

        void UpdateRootPeriodic()
        {
            if (parent != null)
                return;
            var periodicShouldBeActive = (sinks.Count + children.Count) > 0;
            if (periodicShouldBeActive && periodic == null)
            {
                var thisPeriodicVersion = Interlocked.Increment(ref currentVersion);
                periodic = invoker.InvokeAndAwait(async () =>
                {
                    while (true)
                    {
                        await sleep(TimeSpan.FromMilliseconds(500));
                        if (thisPeriodicVersion != Interlocked.Read(ref currentVersion))
                            break;
                        RootUpdate();
                    }
                });
            }
            if (!periodicShouldBeActive && periodic != null)
            {
                periodic = null;
                Interlocked.Increment(ref currentVersion);
            }
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
            info.childrenUpdates?.ForEach(upd => upd.agg.EndUpdate(upd));
            bool active = info.active;
            double progress = info.progress;
            EventHandler<EventArgs> startStop = null;
            if (active != isProgressActive)
                startStop = active ? ProgressStarted : ProgressEnded;
            isProgressActive = active;
            lastValue = active ? progress : new double?();
            startStop?.Invoke(this, EventArgs.Empty);
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
