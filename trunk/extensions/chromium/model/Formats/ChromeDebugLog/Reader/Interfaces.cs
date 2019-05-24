using LogJoint.Analytics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDebugLog
{
	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(string fileName, Action<double> progressHandler = null);
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null);
	}

	public interface IWriter
	{
		Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages);
	};

	[DebuggerDisplay("{Text}")]
	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerThread, ITriggerText
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public StringSlice ProcessId;
		public StringSlice ThreadId;
		public StringSlice Severity;
		public StringSlice File;
		public StringSlice LineNum;
		public readonly string Text;

		int IOrderedTrigger.Index { get { return Index; } }

		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

		string ITriggerThread.ThreadId { get { return ThreadId.Value; } }

		string ITriggerText.Text { get { return Text; } }

		public Message(
			int index, 
			long position,
			StringSlice processId,
			StringSlice threadId,
			DateTime ts,
			StringSlice severity,
			StringSlice file,
			StringSlice lineNum,
			string text
		)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			ProcessId = processId;
			ThreadId = threadId;
			Severity = severity;
			File = file;
			LineNum = lineNum;
			Text = text;
		}
	};

	public struct MessagePrefixesPair
	{
		public readonly Message Message;
		public readonly IMatchedPrefixesCollection Prefixes;

		public MessagePrefixesPair(Message m, IMatchedPrefixesCollection prefixes)
		{
			Message = m;
			Prefixes = prefixes;
		}
	};
}
