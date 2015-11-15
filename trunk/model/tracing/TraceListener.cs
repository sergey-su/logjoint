using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace LogJoint
{
	public class TraceListener : System.Diagnostics.TraceListener
	{
		const string NullStr = "(null)";
		readonly Lazy<TextWriter> writer;
		readonly ConcurrentQueue<Entry> entries = new ConcurrentQueue<Entry>();
		int writeToStreamScheduled;
		bool disposed;

		enum EntryType
		{
			LogMessage,
			Flush,
			Cleanup
		};

		struct Entry
		{
			public EntryType type;
			public DateTime dt;
			public string thread;
			public string message;
			public TraceEventType msgType;
		};

		public TraceListener(string logFileName)
			: base(logFileName)
		{
			writer = new Lazy<TextWriter>(() =>
			{
				try
				{
					return new StreamWriter(logFileName, false);
				}
				catch
				{
					return null;
				}
			}, true);
		}

		public override void Close()
		{
			AddEntry(new Entry() { type = EntryType.Cleanup });
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				this.Close();
			base.Dispose(disposing);
		}

		public override void Flush()
		{
			AddEntry(new Entry() { type = EntryType.Flush });
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
				return;
			AddMessage(eventCache, source, eventType, data != null ? data.ToString() : NullStr);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
				return;
			var messageBuilder = new StringBuilder();
			if (data != null)
			{
				for (int i = 0; i < data.Length; i++)
				{
					if (i != 0)
						messageBuilder.Append(", ");
					if (data[i] != null)
						messageBuilder.Append(data[i].ToString());
				}
			}
			AddMessage(eventCache, source, eventType, messageBuilder.ToString());
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
				return;
			AddMessage(eventCache, source, eventType, message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
				return;
			if (format != null && args != null)
				AddMessage(eventCache, source, eventType, string.Format(CultureInfo.InvariantCulture, format, args));
			else
				AddMessage(eventCache, source, eventType, format);
		}

		public override void Write(string message)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message, null, null, null))
				return;
			AddVerboseMessage(message);
		}

		public override void Write(object obj)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, null, null, obj, null))
				return;
			AddVerboseMessage(obj == null ? NullStr : obj.ToString());
		}

		public override void Write(string message, string category)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, message, null, null, null))
				return;
			AddVerboseMessage((category ?? NullStr) + ": " + (message ?? NullStr));
		}

		public override void Write(object obj, string category)
		{
			if (this.Filter != null && !this.Filter.ShouldTrace(null, "", TraceEventType.Verbose, 0, category, null, obj, null))
				return;
			AddVerboseMessage((category ?? NullStr) + ": " + (obj != null ? obj.ToString() : NullStr));
		}

		protected override void WriteIndent()
		{
		}

		public override void WriteLine(string message)
		{
			Write(message);
		}

		public override void WriteLine(object obj)
		{
			Write(obj);
		}

		public override void WriteLine(string message, string category)
		{
			Write(message, category);
		}

		public override void WriteLine(object obj, string category)
		{
			Write(obj, category);
		}

		void AddVerboseMessage(string message)
		{
			AddMessage(new TraceEventCache(), "", TraceEventType.Verbose, message);
		}

		void AddMessage(TraceEventCache evtCache, string source, TraceEventType eventType, string message)
		{
			AddEntry(new Entry()
			{
				dt = evtCache.DateTime,
				thread = evtCache.ThreadId,
				msgType = eventType,
				message = message
			});
		}

		void AddEntry(Entry e)
		{
			entries.Enqueue(e);
			TryScheduleProcessing();
		}

		void AddMessage()
		{

		}

		void TryScheduleProcessing()
		{
			if (entries.Count > 0)
			{
				if (Interlocked.Exchange(ref writeToStreamScheduled, 1) == 0)
				{
					ThreadPool.QueueUserWorkItem(_ =>
					{
						for (int itemsToProcess = entries.Count; itemsToProcess > 0; --itemsToProcess)
						{
							Entry e;
							if (!entries.TryDequeue(out e))
								break;
							WriteEntry(e);
						}
						Interlocked.Exchange(ref writeToStreamScheduled, 0);
						TryScheduleProcessing();
					});
				}
			}
		}

		void WriteEntry(Entry e)
		{
			if (disposed)
				return;
			if (e.type == EntryType.LogMessage)
			{
				var w = writer.Value;
				if (w == null)
					return;
				w.WriteLine("{0:yyyy/MM/dd HH:mm:ss.fff} T#{1} {2} {3}",
					e.dt,
					e.thread,
					TypeToStr(e.msgType),
					e.message ?? NullStr
				);
			}
			else if (e.type == EntryType.Flush)
			{
				var w = writer.Value;
				if (w == null)
					return;
				w.Flush();
			}
			else if (e.type == EntryType.Cleanup)
			{
				if (writer.IsValueCreated)
					writer.Value.Close();
				disposed = true;
			}
		}

		static string TypeToStr(TraceEventType t)
		{
			switch (t)
			{
				case TraceEventType.Critical: return "C";
				case TraceEventType.Error: return "E";
				case TraceEventType.Warning: return "W";
				case TraceEventType.Information: return "I";
				case TraceEventType.Verbose: return "V";
				case TraceEventType.Start: return "{";
				case TraceEventType.Stop: return "}";
				case TraceEventType.Suspend: return "S";
				case TraceEventType.Resume: return "R";
				case TraceEventType.Transfer: return "T";
				default: return "?";
			}
		}
	}
}
