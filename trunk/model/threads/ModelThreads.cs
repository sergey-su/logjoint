using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class ModelThreads : IModelThreads
	{
		public ModelThreads(IColorTable colors)
		{
			this.colors = colors;
		}
		public ModelThreads()
			: this(new PastelColorsGenerator())
		{
		}

		public event EventHandler OnThreadListChanged;
		public event EventHandler OnThreadVisibilityChanged;
		public event EventHandler OnPropertiesChanged;

		IThread IModelThreads.RegisterThread(string id, ILogSource logSource)
		{
			return new Thread(id, this, logSource);
		}

		IEnumerable<IThread> IModelThreads.Items
		{
			get
			{
				lock (sync)
				{
					for (Thread t = this.threads; t != null; t = t.Next)
						yield return t;
				}
			}
		}

		IThreadsBulkProcessing IModelThreads.StartBulkProcessing()
		{
			return new ThreadsBulkProcessing(this);
		}

		IColorTable IModelThreads.ColorTable
		{
			get { return colors; }
		}

		internal class Thread : IThread, IDisposable
		{
			public bool IsDisposed
			{
				get { return owner == null; }
			}
			public string Description
			{
				get { return description; }
			}
			public string ID
			{
				get { return id; }
			}
			public ModelColor ThreadColor
			{
				get { return color.Color; }
			}
			public ILogSource LogSource
			{
				get { return logSource; }
			}

			public string DisplayName
			{
				get
				{
					string ret;
					if (!string.IsNullOrEmpty(description))
						ret = description;
					else if (!string.IsNullOrEmpty(id))
						ret = id;
					else
						ret = "<no name>";
					if (ret.Length > 200)
						return ret.Substring(0, 200);
					return ret;
				}
			}

			public bool Visible
			{
				get
				{
					return visible;
				}
				set
				{
					CheckDisposed();
					if (visible == value)
						return;
					visible = value;
					if (owner.OnThreadVisibilityChanged != null)
						owner.OnThreadVisibilityChanged(this, EventArgs.Empty);
				}
			}

			public bool ThreadMessagesAreVisible
			{
				get
				{
					if (logSource != null)
						if (!logSource.Visible)
							return false;
					return visible;
				}
			}

			internal void RegisterKnownMessage(IMessage message)
			{
				CheckDisposed();
				if (firstMessageBmk == null || message.Time < firstMessageBmk.Time)
				{
					firstMessageBmk = new Bookmark(message, 0, true);
					description = ComposeDescriptionFromTheFirstKnownLine(message);
				}
				if (message.Time >= lastMessageTime)
				{
					lastMessageTime = message.Time;

					lastMessageBmk = null; // invalidate last message bookmark
					lastMessageBmkMessage = message; // store last message to be able to create lastMessageBmk later
				}
				if (owner.OnPropertiesChanged != null)
					owner.OnPropertiesChanged(this, EventArgs.Empty);
			}

			public IBookmark FirstKnownMessage 
			{
				get { return firstMessageBmk; }
			}
			public IBookmark LastKnownMessage 
			{
				get 
				{
					if (lastMessageBmk == null)
					{
						if (lastMessageBmkMessage != null)
						{
							lastMessageBmk = new Bookmark(lastMessageBmkMessage, 0, true);
							lastMessageBmkMessage = null;
						}
					}
					return lastMessageBmk; 
				}
			}

			public void Dispose()
			{
				if (owner != null)
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
					}
					owner.colors.ReleaseColor(color.ID);
					ModelThreads tmp = owner;
					EventHandler tmpEvt = tmp.OnThreadListChanged;
					owner = null;
					if (tmpEvt != null)
						tmpEvt(tmp, EventArgs.Empty);
				}
			}

			public override string ToString()
			{
				return string.Format("{0}. {1}", id, String.IsNullOrEmpty(description) ? "<no name>" : description);
			}

			public Thread(string id, ModelThreads owner, ILogSource logSource)
			{
				this.id = id;
				this.visible = true;
				this.owner = owner;
				this.color = owner.colors.GetNextColor(true);
				this.logSource = logSource;

				lock (owner.sync)
				{
					next = owner.threads;
					owner.threads = this;
					if (next != null)
						next.prev = this;
				}
				if (owner.OnThreadListChanged != null)
					owner.OnThreadListChanged(owner, EventArgs.Empty);
			}

			public Thread Next { get { return next; } }

			void CheckDisposed()
			{
				if (IsDisposed)
					throw new ObjectDisposedException(this.ToString());
			}

			string ComposeDescriptionFromTheFirstKnownLine(IMessage firstKnownLine)
			{
				return string.Format("{0}. {1}", this.ID, firstKnownLine.TextAsMultilineText.GetNthTextLine(0));
			}

			readonly ILogSource logSource;
			ModelThreads owner;
			Thread next, prev;
			string id;
			bool visible;
			string description;
			ColorTableEntry color;
			IBookmark firstMessageBmk;
			MessageTimestamp lastMessageTime;
			IMessage lastMessageBmkMessage;
			IBookmark lastMessageBmk;
		};

		internal class ThreadsBulkProcessing : IThreadsBulkProcessing
		{
			public ThreadsBulkProcessing(ModelThreads owner)
			{
			}

			public void Dispose()
			{
			}

			public ThreadsBulkProcessingResult ProcessMessage(IMessage message)
			{
				if (!message.Thread.IsDisposed)
				{
					var threadInfo = GetThreadInfo(message.Thread);
					threadInfo.ThreadImpl.RegisterKnownMessage(message);
				}
				return new ThreadsBulkProcessingResult() { 
				};
			}

			ThreadInfo GetThreadInfo(IThread thread)
			{
				if (threads.TryGetValue(thread, out ThreadInfo threadInfo))
					return threadInfo;
				threadInfo = new ThreadInfo() { ThreadImpl = (Thread)thread };
				threads.Add(thread, threadInfo);
				return threadInfo;
			}

			internal class ThreadInfo
			{
				public Thread ThreadImpl;
			};
			readonly Dictionary<IThread, ThreadInfo> threads = new Dictionary<IThread, ThreadInfo>();
		};

		static byte Inc(byte v)
		{
			byte delta = 16;
			if (255 - v <= delta)
				return 255;
			return (byte)(v + delta);
		}

		object sync = new object();
		Thread threads;
		IColorTable colors;
	}
}
