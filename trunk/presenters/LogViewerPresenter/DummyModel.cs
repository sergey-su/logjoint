using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
    public class DummyModel : IModel
    {
        DummySource dummySource;

        public DummyModel()
        {
            this.dummySource = new DummySource();
        }

        public void SetMessages(IEnumerable<IMessage> msgs)
        {
            dummySource = new DummySource();
            foreach (var m in msgs)
                dummySource.messages.Add(m);
            OnSourcesChanged?.Invoke(this, EventArgs.Empty);
        }

        event EventHandler<SourceMessagesChangeArgs> IModel.OnSourceMessagesChanged
        {
            add { }
            remove { }
        }

        public event EventHandler OnSourcesChanged;

        IEnumerable<IMessagesSource> IModel.Sources
        {
            get { yield return dummySource; }
        }

        event EventHandler IModel.OnLogSourceColorChanged
        {
            add { }
            remove { }
        }

        public class DummySource : LogViewer.IMessagesSource
        {
            public MessagesContainers.ListBasedCollection messages = new MessagesContainers.ListBasedCollection();
            public ILogSource logSourceHint = null;

            Task<DateBoundPositionResponseData> IMessagesSource.GetDateBoundPosition(DateTime d, ValueBound bound, LogProviderCommandPriority priority, System.Threading.CancellationToken cancellation)
            {
                return Task.FromResult(messages.GetDateBoundPosition(d, bound));
            }

            Task IMessagesSource.EnumMessages(long fromPosition, Func<IMessage, bool> callback,
                EnumMessagesFlag flags, LogProviderCommandPriority priority, CancellationToken cancellation)
            {
                messages.EnumMessages(fromPosition, callback, flags);
                return Task.FromResult(0);
            }

            FileRange.Range IMessagesSource.PositionsRange
            {
                get { return messages.PositionsRange; }
            }

            DateRange IMessagesSource.DatesRange
            {
                get { return messages.DatesRange; }
            }

            FileRange.Range LogViewer.IMessagesSource.ScrollPositionsRange
            {
                get { return messages.PositionsRange; }
            }

            long LogViewer.IMessagesSource.MapPositionToScrollPosition(long pos)
            {
                return pos;
            }

            long LogViewer.IMessagesSource.MapScrollPositionToPosition(long pos)
            {
                return pos;
            }

            ILogSource LogViewer.IMessagesSource.LogSourceHint
            {
                get { return logSourceHint; }
            }

            bool LogViewer.IMessagesSource.HasConsecutiveMessages
            {
                get { return true; }
            }
        };
    };
};