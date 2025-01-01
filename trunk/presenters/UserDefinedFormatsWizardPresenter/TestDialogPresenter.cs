using System.Text;
using System.Collections.Generic;
using System;
using System.Threading;

namespace LogJoint.UI.Presenters.FormatsWizard.TestDialog
{
    internal class Presenter : IPresenter, IDisposable, IViewEvents, ILogProviderHost
    {
        readonly IView view;

        readonly IModelThreadsInternal threads;
        readonly ILogSourceThreadsInternal logSourceThreads;
        ILogProvider provider;
        LogViewer.DummyModel model;
        LogViewer.IPresenterInternal logPresenter;
        bool statusOk;

        public Presenter(
            IView view,
            LogViewer.IPresenterFactory logViewerPresenterFactory
        )
        {
            this.view = view;
            this.view.SetEventsHandler(this);

            this.threads = new ModelThreads();
            this.logSourceThreads = new LogSourceThreads(
                LJTraceSource.EmptyTracer, threads, null);
            this.model = new Presenters.LogViewer.DummyModel();
            this.logPresenter = logViewerPresenterFactory.CreateIsolatedPresenter(model, view.LogViewer);
            logPresenter.ShowTime = true;
            logPresenter.EmptyViewMessageAllowed = false;
        }

        void IDisposable.Dispose()
        {
            view.Dispose();
            logSourceThreads.Dispose();
            logPresenter.Dispose();
        }

        void IViewEvents.OnCloseButtonClicked()
        {
            view.Close();
        }

        bool IPresenter.ShowDialog(ILogProviderFactory sampleLogFactory, IConnectionParams sampleLogConnectionParams)
        {
            ReadAll(sampleLogFactory, sampleLogConnectionParams);

            view.Show();
            return statusOk;
        }

        string ILogProviderHost.LoggingPrefix => "test";

        ITimeOffsets ILogProviderHost.TimeOffsets
        {
            get { return TimeOffsets.Empty; }
        }

        ILogSourceThreads ILogProviderHost.Threads
        {
            get { return logSourceThreads; }
        }

        void ILogProviderHost.OnStatisticsChanged(
            LogProviderStats value, LogProviderStats oldValue, LogProviderStatsFlag flags)
        {
        }

        private async void ReadAll(
            ILogProviderFactory factory,
            IConnectionParams connectParams)
        {
            try
            {
                provider = await factory.CreateFromConnectionParams(this, connectParams);

                var messages = new List<IMessage>();
                await this.provider.EnumMessages(
                    0, m =>
                    {
                        messages.Add(m);
                        return true;
                    },
                    EnumMessagesFlag.Forward,
                    LogProviderCommandPriority.RealtimeUserAction,
                    CancellationToken.None
                );
                model.SetMessages(messages);
                await logPresenter.GoHome();
                UpdateStatusControls(messages.Count, null);
            }
            catch (Exception e)
            {
                UpdateStatusControls(0, e);
            }
        }

        private void UpdateStatusControls(int messagsCount, Exception e)
        {
            StringBuilder msg = new StringBuilder();
            bool? success = null;
            if (e != null)
            {
                while (e.InnerException != null)
                    e = e.InnerException;
                msg.AppendFormat("Failed to parse sample log: {0}", e.Message);
                success = false;
            }
            else
            {
                LogProviderStats s = provider.Stats;
                switch (s.State)
                {
                    case LogProviderState.Idle:
                    case LogProviderState.DetectingAvailableTime:
                    case LogProviderState.NoFile:
                        if (messagsCount > 0)
                        {
                            success = true;
                            msg.AppendFormat("Successfully parsed {0} message(s)", messagsCount);
                        }
                        else
                        {
                            if (s.State == LogProviderState.Idle)
                            {
                                msg.Append("No messages parsed");
                                success = false;
                            }
                            else
                            {
                                msg.Append("Trying to parse...");
                            }
                        }
                        break;
                    case LogProviderState.LoadError:
                        msg.AppendFormat("{0}", s.Error.Message);
                        success = false;
                        break;
                }
            }

            var testOutcome = TestOutcome.None;
            if (success.HasValue)
                if (success.Value)
                    testOutcome = TestOutcome.Success;
                else
                    testOutcome = TestOutcome.Failure;

            statusOk = success.GetValueOrDefault(false);

            view.SetData(msg.ToString(), testOutcome);
        }
    };
};