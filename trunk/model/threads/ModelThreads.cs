using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
    public class ModelThreads : IModelThreads, IModelThreadsInternal
    {
        public ModelThreads(IColorLease colors)
        {
            this.colors = colors;
        }
        public ModelThreads()
            : this(new ColorLease(16))
        {
        }

        public event EventHandler OnThreadListChanged;
        public event EventHandler OnThreadPropertiesChanged;

        IThread IModelThreadsInternal.RegisterThread(string id, ILogSource logSource)
        {
            return new Thread(id, this, logSource);
        }

        void IModelThreadsInternal.UnregisterThread(IThread thread)
        {
            ((Thread)thread).Dispose();
        }

        IReadOnlyList<IThread> IModelThreads.Items
        {
            get
            {
                var result = new List<IThread>();
                lock (sync)
                {
                    for (Thread t = this.threads; t != null; t = t.Next)
                        result.Add(t);
                }
                return result;
            }
        }

        internal class Thread : IThread
        {
            bool IThread.IsDisposed => IsDisposed;

            string IThread.Description => GetDescription();

            string IThread.ID => id;

            int IThread.ThreadColorIndex => color;

            ILogSource IThread.LogSource => logSource;

            string IThread.DisplayName
            {
                get
                {
                    var desc = GetDescription();
                    string ret;
                    if (!string.IsNullOrEmpty(desc))
                        ret = desc;
                    else if (!string.IsNullOrEmpty(id))
                        ret = id;
                    else
                        ret = "<no name>";
                    if (ret.Length > 200)
                        return ret.Substring(0, 200);
                    return ret;
                }
            }

            void IThread.RegisterKnownMessage(IMessage message)
            {
                if (IsDisposed)
                    return;
                bool changed = false;
                if (firstMessage == null || message.Position < firstMessage.Position)
                {
                    firstMessage = message;
                    firstMessageBmk = null;
                    description = null;
                    changed = true;
                }
                if (lastMessage == null || message.Position > lastMessage.Position)
                {
                    lastMessageBmk = null; // invalidate last message bookmark
                    lastMessage = message; // store last message to be able to create lastMessageBmk later
                    changed = true;
                }
                if (changed)
                {
                    owner.OnThreadPropertiesChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            IBookmark? IThread.FirstKnownMessage
            {
                get
                {
                    if (firstMessageBmk == null && firstMessage != null)
                    {
                        firstMessageBmk = new Bookmark(firstMessage, 0, true);
                    }
                    return firstMessageBmk;
                }
            }

            IBookmark? IThread.LastKnownMessage
            {
                get
                {
                    if (lastMessageBmk == null && lastMessage != null)
                    {
                        lastMessageBmk = new Bookmark(lastMessage, 0, true);
                    }
                    return lastMessageBmk;
                }
            }

            internal void Dispose()
            {
                if (!IsDisposed)
                {
                    lock (owner.sync)
                    {
                        if (this == owner.threads)
                        {
                            owner.threads = next;
                            if (next != null)
                                next.prev = null;
                        }
                        else
                        {
                            prev.next = next;
                            if (next != null)
                                next.prev = prev;
                        }
                        owner.colors.ReleaseColor(color);
                    }
                    ModelThreads tmp = owner;
                    EventHandler tmpEvt = tmp.OnThreadListChanged;
                    owner = null;
                    tmpEvt?.Invoke(tmp, EventArgs.Empty);
                }
            }

            public override string ToString()
            {
                var desc = GetDescription();
                return string.Format("{0}. {1}", id, String.IsNullOrEmpty(desc) ? "<no name>" : desc);
            }

            public Thread(string id, ModelThreads owner, ILogSource logSource)
            {
                this.id = id;
                this.owner = owner;
                this.logSource = logSource;

                lock (owner.sync)
                {
                    color = owner.colors.GetNextColor();
                    next = owner.threads;
                    owner.threads = this;
                    if (next != null)
                        next.prev = this;
                }
                owner.OnThreadListChanged?.Invoke(owner, EventArgs.Empty);
            }

            internal Thread Next => next;

            bool IsDisposed => owner == null;

            string ComposeDescriptionFromTheFirstKnownLine(IMessage firstKnownLine)
            {
                return string.Format("{0}. {1}", this.id, firstKnownLine.TextAsMultilineText.GetNthTextLine(0));
            }

            private string GetDescription()
            {
                if (description == null && firstMessage != null)
                {
                    description = ComposeDescriptionFromTheFirstKnownLine(firstMessage);
                }
                return description ?? "";
            }

            readonly ILogSource logSource;
            readonly string id;
            readonly int color;
            ModelThreads owner;
            Thread next, prev;
            string? description;
            IMessage firstMessage;
            IBookmark? firstMessageBmk;
            IMessage lastMessage;
            IBookmark? lastMessageBmk;
        };

        object sync = new object();
        Thread threads;
        IColorLease colors;
    }
}
