using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace LogJoint
{
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

		static Color MakeLighter(Color cl)
		{
			return Color.FromArgb(255, Inc(cl.R), Inc(cl.G), Inc(cl.B));
		}		

		public Color GenerateNewThreadColor()
		{
			return colors.GenerateNewColor();
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
			public bool IsInitialized
			{
				get { return !string.IsNullOrEmpty(descr); }
			}
			public bool IsDisposed
			{
				get { return owner == null; }
			}
			public string Description
			{
				get { return descr; }
			}
			public string ID
			{
				get { return id; }
			}
			public Color ThreadColor
			{
				get { return color; }
			}
			public Brush ThreadBrush
			{
				get { return brush; }
			}
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
					if (!string.IsNullOrEmpty(descr))
						return descr;
					if (!string.IsNullOrEmpty(id))
						return id;
					return "<no name>";
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
				++collapsedRegionDepth;
			}

			public void EndCollapsedRegion()
			{
				--collapsedRegionDepth;
			}

			public bool IsInCollapsedRegion 
			{ 
				get { return collapsedRegionDepth != 0; } 
			}

			public void BeginFilterRegion(Filter filter)
			{
				if (filterRegionDepth == 0)
					regionFilter = filter;
				else
					System.Diagnostics.Debug.Assert(filter == regionFilter);
				++filterRegionDepth;
			}

			public void EndFilterRegion()
			{
				--filterRegionDepth;
				if (filterRegionDepth == 0)
					regionFilter = null;
			}

			public Filter RegionFilter 
			{
				get { return regionFilter; } 
			}

			public void CountLine(MessageBase line)
			{
				messagesCount++;
				if (firstMessage == null || line.Time < firstMessage.Time)
					firstMessage = new Bookmark(line);
				if (lastMessage == null || line.Time >= lastMessage.Time)
					lastMessage = new Bookmark(line);
				if (owner.OnPropertiesChanged != null)
					owner.OnPropertiesChanged(this, EventArgs.Empty);
			}

			public void ResetCounters(ThreadCounter counterFlags)
			{
				if ((counterFlags & ThreadCounter.FramesInfo) != 0)
				{
					frames.Clear();
					collapsedRegionDepth = 0;
				}
				if ((counterFlags & ThreadCounter.FilterRegions) != 0)
				{
					filterRegionDepth = 0;
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
				get { return firstMessage; }
			}
			public IBookmark LastKnownMessage 
			{ 
				get { return lastMessage; }
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
					Threads tmp = owner;
					EventHandler tmpEvt = tmp.OnThreadListChanged;
					owner = null;
					if (tmpEvt != null)
						tmpEvt(tmp, EventArgs.Empty);
				}
			}

			public override string ToString()
			{
				return string.Format("{0}. {1}", ID, String.IsNullOrEmpty(Description) ? "<no name>" : Description);
			}

			public Thread(string id, Threads owner, ILogSource logSource)
			{
				this.id = id;
				this.visible = true;
				this.owner = owner;
				this.color = owner.GenerateNewThreadColor();
				this.brush = new SolidBrush(color);
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

			public void Init(string descr)
			{
				this.descr = descr;
				if (owner.OnPropertiesChanged != null)
					owner.OnPropertiesChanged(this, EventArgs.Empty);
			}

			public Thread Next { get { return next; } }

			string descr;
			string id;
			Color color;
			Brush brush;
			bool visible;
			int collapsedRegionDepth;
			int filterRegionDepth;
			Filter regionFilter;
			int messagesCount;
			IBookmark firstMessage, lastMessage;
			Stack<MessageBase> frames = new Stack<MessageBase>();
			Thread next, prev;
			Threads owner;
			ILogSource logSource;
		};

		object sync = new object();
		Thread threads;
		PastelColorsGenerator colors = new PastelColorsGenerator();
	}
}
