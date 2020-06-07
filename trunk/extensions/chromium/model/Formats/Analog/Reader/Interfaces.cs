using LogJoint.Postprocessing;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Google.Analog
{
	[DebuggerDisplay("{Text}")]
	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerThread, ITriggerText
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public StringSlice ThreadId;
		public StringSlice ThreadName;
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
			StringSlice threadId,
			StringSlice threadName,
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
			ThreadName = threadName;
			ThreadId = threadId;
			Severity = severity;
			File = file;
			LineNum = lineNum;
			Text = text;
		}
	};

	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(string fileName, Action<double> progressHandler = null);
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null);
	}
}
