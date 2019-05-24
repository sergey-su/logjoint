using LogJoint.Analytics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Symphony.SpringServiceLog
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
	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerText
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public StringSlice Severity;
		public StringSlice Pid;
		public StringSlice Tid;
		public StringSlice Logger;
		public readonly string Text;

		int IOrderedTrigger.Index { get { return Index; } }

		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

		string ITriggerText.Text { get { return Text; } }

		public Message(
			int index, 
			long position,
			DateTime ts,
			StringSlice severity,
			StringSlice pid,
			StringSlice tid,
			StringSlice logger,
			string text
		)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			Severity = severity;
			Pid = pid;
			Tid = tid;
			Logger = logger;
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
