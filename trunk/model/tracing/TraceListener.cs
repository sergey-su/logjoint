using System;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LogJoint
{
	public class TraceListener : System.Diagnostics.TraceListener
	{
		const string NullStr = "(null)";
		readonly Lazy<TextWriter> writer;
		readonly ConcurrentQueue<Entry> entries = new ConcurrentQueue<Entry>();
		int writeToStreamScheduled;
		bool disposed;
		readonly bool enableMemBuffer;
		readonly bool enableConsole;
		readonly uint memBufMaxSize = 128 * 1024;
		CircularBuffer memBuffer;
		static TraceListener lastInstance;
		readonly bool enableLogicalThread;

		class InitializationParams
		{
			public readonly string FileName;
			public readonly bool EnableMemBuffer;
			public readonly bool EnableConsole;
			public readonly bool EnableLogicalThread;
		
			public InitializationParams(string str)
			{
				var split = str.Split(new [] {';'}, StringSplitOptions.None);
				if (split.Length > 0)
					FileName = Environment.ExpandEnvironmentVariables(split[0]);
				foreach (var arg in split.Skip(1))
				{
					var argSplit = arg.Split('=');
					if (argSplit.Length != 2)
						continue;
					if (argSplit[0] == "membuf")
						EnableMemBuffer = argSplit[1]=="1";
					else if (argSplit[0] == "console")
						EnableConsole = argSplit[1] == "1";
					else if (argSplit[0] == "logical-thread")
						EnableLogicalThread = argSplit[1] == "1";
				}
			}
		};

		public enum EntryType
		{
			None,
			LogMessage,
			Flush,
			Cleanup
		};

		public struct Entry
		{
			public EntryType type;
			public DateTime dt;
			public string thread;
			public string logicalThread;
			public string message;
			public TraceEventType msgType;

			public void Write(TextWriter w)
			{
				w.WriteLine("{0:yyyy'/'MM'/'dd HH':'mm':'ss'.'fff} T#{1}{4} {2} {3}",
					dt,
					thread,
					TypeToStr(msgType),
					message ?? NullStr,
					logicalThread != null ? $" L#{logicalThread}" : ""
				);
			}
		};

		public TraceListener(string initializationParams): 
			this(new InitializationParams(initializationParams))
		{
		}
		
		public static TraceListener LastInstance
		{
			get { return lastInstance; }
		}

		public bool MemBufferEnabled
		{
			get { return enableMemBuffer; }
		}

		public List<Entry> ClearMemBufferAndGetCurrentEntries()
		{
			if (!enableMemBuffer)
				return null;
			return Interlocked.Exchange(ref memBuffer, new CircularBuffer(memBufMaxSize)).ToList();
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

		private TraceListener(InitializationParams initializationParams)
			: base(initializationParams.FileName)
		{
			writer = new Lazy<TextWriter>(() =>
			{
				if (string.IsNullOrEmpty(initializationParams.FileName))
				{
					return null;
				}
				try
				{
					return new StreamWriter(initializationParams.FileName, false);
				}
				catch
				{
					return null;
				}
			}, true);
			if (initializationParams.EnableMemBuffer)
			{
				enableMemBuffer = true;
				memBuffer = new CircularBuffer(memBufMaxSize);
			}
			enableConsole = initializationParams.EnableConsole;
			this.enableLogicalThread = initializationParams.EnableLogicalThread;

			lastInstance = this;
		}

		void AddVerboseMessage(string message)
		{
			AddMessage(new TraceEventCache(), "", TraceEventType.Verbose, message);
		}

		void AddMessage(TraceEventCache evtCache, string source, TraceEventType eventType, string message)
		{
			AddEntry(new Entry()
			{
				type = EntryType.LogMessage,
				dt = evtCache.DateTime,
				thread =
					RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? // on mono TraceEventCache.ThreadId does not return ID but thread name
						Thread.CurrentThread.ManagedThreadId.ToString() :
						evtCache.ThreadId,
				logicalThread = enableLogicalThread ? (SynchronizationContext.Current?.ToString () ?? "NA") : null,
				msgType = eventType,
				message = message
			});
		}

		void AddEntry(Entry e)
		{
			entries.Enqueue(e);
			TryScheduleProcessing();
			AddEntryToMemBuffer(e);
			MaybeLogEntryToConsole(e);
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

		void AddEntryToMemBuffer(Entry e)
		{
			var tmp = memBuffer;
			if (tmp != null && e.type == EntryType.LogMessage)
			{
				tmp.Push(e);
			}
		}

		void MaybeLogEntryToConsole(Entry e)
		{
			if (enableConsole)
			{
				e.Write(Console.Out);
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
				e.Write(w);
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

		class CircularBuffer
		{
			readonly Entry[] entries;
			readonly uint size;
			int pos = -1;

			public CircularBuffer(uint size)
			{
				this.entries = new Entry[size];
				this.size = size;
			}

			public void Push(Entry e)
			{
				var i = Interlocked.Increment(ref pos);
				entries[unchecked((uint)(i)) % size] = e;
			}

			public List<Entry> ToList()
			{
				var ret = new List<Entry>();
				var i = (int)(unchecked((uint)(pos + 1)) % size);
				ret.AddRange(entries.EnumForward(0, i));
				ret.AddRange(entries.EnumForward(i, entries.Length).Where(e => e.type != EntryType.None));
				return ret;
			}
		}
	}
}
