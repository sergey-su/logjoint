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
#if !SILVERLIGHT
			public System.Drawing.Brush ThreadBrush
			{
				get { CheckDisposed(); return brush; }
			}
#endif
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
					firstMessageBmk = new Bookmark(message);
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
							lastMessageBmk = new Bookmark(lastMessageBmkMessage);
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
#if !SILVERLIGHT
					brush.Dispose();
#endif
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
#if !SILVERLIGHT
				this.brush = new System.Drawing.SolidBrush(color.Color.ToColor());
#endif
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
				return string.Format("{0}. {1}", this.ID, firstKnownLine.Text);
			}

			readonly ILogSource logSource;
			ModelThreads owner;
			Thread next, prev;
			string id;
			bool visible;
			string description;
			ColorTableEntry color;
#if !SILVERLIGHT
			System.Drawing.Brush brush;
#endif
			IBookmark firstMessageBmk;
			MessageTimestamp lastMessageTime;
			IMessage lastMessageBmkMessage;
			IBookmark lastMessageBmk;
		};

		internal class ThreadsBulkProcessing : IThreadsBulkProcessing
		{
			public ThreadsBulkProcessing(ModelThreads owner)
			{
				this.owner = owner;
				foreach (ThreadInfo t in threads.Values)
					t.ResetFrames();
			}

			public void Dispose()
			{
			}

			public ThreadsBulkProcessingResult ProcessMessage(IMessage message)
			{
				var threadInfo = GetThreadInfo(message.Thread);
				bool wasInCollapsedRegion = threadInfo.collapsedRegionDepth != 0;
				threadInfo.ThreadImpl.RegisterKnownMessage(message);
				HandleFrameFlagsAndSetLevel(threadInfo, message);
				return new ThreadsBulkProcessingResult() { 
					info = threadInfo,
					threadWasInCollapsedRegion = wasInCollapsedRegion,
					threadIsInCollapsedRegion = threadInfo.collapsedRegionDepth != 0
				};
			}

			void HandleFrameFlagsAndSetLevel(ThreadInfo td, IMessage message)
			{
				MessageFlag f = message.Flags;
				int level = td.frames.Count;
				switch (f & MessageFlag.TypeMask)
				{
					case MessageFlag.StartFrame:
						td.frames.Push(message);
						if ((f & MessageFlag.Collapsed) != 0)
							++td.collapsedRegionDepth;
						break;
					case MessageFlag.EndFrame:
						var end = (IFrameEnd)message;
						if (td.frames.Count > 0)
						{
							var begin = (IFrameBegin)td.frames.Pop();
							end.SetStart(begin);
							begin.SetEnd(end);
							--level;
						}
						else
						{
							thereAreHangingEndFrames = true;
							end.SetStart(null);
						}
						if ((f & MessageFlag.Collapsed) != 0)
							--td.collapsedRegionDepth;
						break;
				}

				message.SetLevel(level);
			}

			public void HandleHangingFrames(IMessagesCollection messagesCollection)
			{
				if (!thereAreHangingEndFrames)
					return;

				foreach (ThreadInfo t in threads.Values)
					t.ResetFrames();

				foreach (IndexedMessage r in messagesCollection.Reverse(int.MaxValue, -1))
				{
					ThreadInfo t = GetThreadInfo(r.Message.Thread);
					r.Message.SetLevel(r.Message.Level + t.frames.Count);

					var fe = r.Message as IFrameEnd;
					if (fe != null && fe.Start == null)
						t.frames.Push(r.Message);
				}
			}

			readonly ModelThreads owner;
			/// <summary>
			/// Flag that will indicate that there are ending frames that don't 
			/// have appropriate begining frames. Such end frames can appear 
			/// if the log is not loaded completely and some begin frames are
			/// before the the loaded log range.
			/// </summary>
			bool thereAreHangingEndFrames;

			ThreadInfo GetThreadInfo(IThread thread)
			{
				ThreadInfo threadInfo;
				if (threads.TryGetValue(thread, out threadInfo))
					return threadInfo;
				threadInfo = new ThreadInfo() { ThreadImpl = (Thread)thread };
				threads.Add(thread, threadInfo);
				return threadInfo;
			}

			internal class ThreadInfo
			{
				public Thread ThreadImpl;
				public readonly Stack<IMessage> frames = new Stack<IMessage>();
				public int collapsedRegionDepth;
				public readonly FilterContext displayFilterContext = new FilterContext();
				public readonly FilterContext highlightFilterContext = new FilterContext();

				public void ResetFrames()
				{
					frames.Clear();
					collapsedRegionDepth = 0;
				}
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
