using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics.InternalTrace
{
	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, string fileNameHint = null, Action<double> progressHandler = null);
	}

	public interface IWriter
	{
		Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages);
	};

	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerThread
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public readonly StringSlice Thread;
		public readonly StringSlice Level;
		public readonly StringSlice Source;
		public readonly string Text;

		int IOrderedTrigger.Index { get { return Index; } }
		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }
		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }
		string ITriggerThread.ThreadId { get { return Thread.Value; } }

		public Message(int index, long position, DateTime ts, StringSlice thread, 
			StringSlice level, StringSlice source, string text)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			Thread = thread;
			Level = level;
			Source = source;
			Text = text;
		}
	};
}
