using System;
using System.ComponentModel;

namespace LogJoint.Progress
{
    public interface IProgressEventsSink : IDisposable
    {
        void SetValue(double value);
    }

    public interface IProgressAggregator : IDisposable
    {
        IProgressEventsSink CreateProgressSink();

        IProgressAggregator CreateChildAggregator();

        double? ProgressValue { get; }

        event EventHandler<EventArgs> ProgressStarted;
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        event EventHandler<EventArgs> ProgressEnded;
    };

    public interface IProgressAggregatorFactory
    {
        IProgressAggregator CreateProgressAggregator();
    };
}
