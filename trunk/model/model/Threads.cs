using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class FilterContext
	{
		public void Reset()
		{
			filterRegionDepth = 0;
			regionFilter = null;
		}

		public void BeginRegion(Filter filter)
		{
			if (filterRegionDepth == 0)
				regionFilter = filter;
			else
				System.Diagnostics.Debug.Assert(filter == regionFilter);
			++filterRegionDepth;
		}

		public void EndRegion()
		{
			--filterRegionDepth;
			if (filterRegionDepth == 0)
				regionFilter = null;
		}

		public Filter RegionFilter
		{
			get { return regionFilter; }
		}

		int filterRegionDepth;
		Filter regionFilter;
	};

	public interface IThread : IDisposable
	{
		bool IsDisposed { get; }
		string ID { get; }
		string Description { get; }
		string DisplayName { get; }
		bool Visible { get; set; }
		bool ThreadMessagesAreVisible { get; }
		ModelColor ThreadColor { get; }
#if !SILVERLIGHT
		System.Drawing.Brush ThreadBrush { get; }
#endif
		int MessagesCount { get; }
		IBookmark FirstKnownMessage { get; }
		IBookmark LastKnownMessage { get; }
		ILogSource LogSource { get; }

		Stack<MessageBase> Frames { get; }

		void BeginCollapsedRegion();
		void EndCollapsedRegion();
		bool IsInCollapsedRegion { get; }

		FilterContext DisplayFilterContext { get; }
		FilterContext HighlightFilterContext { get; }

		void ResetCounters(ThreadCounter counterFlags);
		void CountLine(MessageBase line);
	}

	[Flags]
	public enum ThreadCounter
	{
		None = 0,
		Messages = 1,
		FramesInfo = 2,
		FilterRegions = 4,
		All = Messages | FramesInfo | FilterRegions,
	};

	public class Threads
	{
		public Threads()
		{
		}

		public event EventHandler OnThreadListChanged;
		public event EventHandler OnThreadVisibilityChanged;
		public event EventHandler OnPropertiesChanged;

		static byte Inc(byte v)
		{
			byte delta = 16;
			if (255 - v <= delta)
				return 255;
			return (byte)(v + delta);
		}

		public IThread RegisterThread(string id, ILogSource logSource)
		{
			return new Thread(id, this, logSource);
		}

		public IEnumerable<IThread> Items
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

		class Thread : IThread, IDisposable
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
			public int MessagesCount
			{
				get { return messagesCount; }
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

			public Stack<MessageBase> Frames
			{
				get { return frames; }
			}

			public void BeginCollapsedRegion()
			{
				CheckDisposed();
				++collapsedRegionDepth;
			}

			public void EndCollapsedRegion()
			{
				CheckDisposed();
				--collapsedRegionDepth;
			}

			public bool IsInCollapsedRegion 
			{
				get { return collapsedRegionDepth != 0; } 
			}

			public FilterContext DisplayFilterContext { get { return displayFilterContext; } }
			public FilterContext HighlightFilterContext { get { return highlightFilterContext; } }

			public void CountLine(MessageBase line)
			{
				CheckDisposed();
				messagesCount++;
				if (firstMessageBmk == null || line.Time < firstMessageBmk.Time)
				{
					firstMessageBmk = new Bookmark(line);
					description = ComposeDescriptionFromTheFirstKnownLine(line);
				}
				if (line.Time >= lastMessageTime)
				{
					lastMessageTime = line.Time;

					lastMessageBmk = null; // invalidate last message bookmark
					lastMessageBmkMessage = line; // store last message to be able to create lastMessageBmk later
				}
				if (owner.OnPropertiesChanged != null)
					owner.OnPropertiesChanged(this, EventArgs.Empty);
			}

			public void ResetCounters(ThreadCounter counterFlags)
			{
				CheckDisposed();
				if ((counterFlags & ThreadCounter.FramesInfo) != 0)
				{
					frames.Clear();
					collapsedRegionDepth = 0;
				}
				if ((counterFlags & ThreadCounter.FilterRegions) != 0)
				{
					displayFilterContext.Reset();
					displayFilterContext.Reset();
				}
				if ((counterFlags & ThreadCounter.Messages) != 0)
				{
					messagesCount = 0;
				}

				if (counterFlags != ThreadCounter.None
				 && owner.OnPropertiesChanged != null)
				{
					owner.OnPropertiesChanged(this, EventArgs.Empty);
				}
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
					Threads tmp = owner;
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

			public Thread(string id, Threads owner, ILogSource logSource)
			{
				this.id = id;
				this.visible = true;
				this.owner = owner;
				this.color = owner.colors.GetNextColor(true);
#if !SILVERLIGHT
				this.brush = new System.Drawing.SolidBrush(color.Color.ToColor());
#endif
				this.logSource = logSource;
				this.displayFilterContext = new FilterContext();
				this.highlightFilterContext = new FilterContext();

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

			string ComposeDescriptionFromTheFirstKnownLine(MessageBase firstKnownLine)
			{
				return string.Format("{0}. {1}", this.ID, firstKnownLine.Text);
			}

			string description;
			string id;
			ColorTableBase.ColorTableEntry color;
#if !SILVERLIGHT
			System.Drawing.Brush brush;
#endif
			bool visible;
			int collapsedRegionDepth;
			int messagesCount;
			IBookmark firstMessageBmk;
			DateTime lastMessageTime;
			MessageBase lastMessageBmkMessage;
			IBookmark lastMessageBmk;
			readonly Stack<MessageBase> frames = new Stack<MessageBase>();
			Thread next, prev;
			Threads owner;
			readonly ILogSource logSource;
			readonly FilterContext displayFilterContext;
			readonly FilterContext highlightFilterContext;
		};

		object sync = new object();
		Thread threads;
		PastelColorsGenerator colors = new PastelColorsGenerator();
	}
}
