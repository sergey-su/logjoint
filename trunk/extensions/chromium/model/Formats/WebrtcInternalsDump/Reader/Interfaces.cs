using LogJoint.Analytics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, string logFileNameHint = null, Action<double> progressHandler = null);
	}

	public interface IWriter
	{
		Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages);
	};

	[DebuggerDisplay("{ObjectId}.{PropName}={PropValue}")]
	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerText
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public readonly StringSlice RootObjectType;
		public readonly StringSlice RootObjectId;
		public readonly StringSlice ObjectId;
		public readonly StringSlice PropName;
		public readonly StringSlice PropValue;
		public readonly StringSlice Text;

		int IOrderedTrigger.Index { get { return Index; } }

		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

		string ITriggerText.Text { get { return Text.Value; } }

		public static class RootObjectTypes
		{
			public static StringSlice Connection = new StringSlice("C");
			public static StringSlice UserMediaRequest = new StringSlice("M");
		}

		public Message(
			int index, 
			long position,
			DateTime ts,
			StringSlice rootObjectType,
			StringSlice rootObjectId,
			StringSlice objectId,
			StringSlice propName,
			StringSlice propValue,
			StringSlice text
		)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			RootObjectType = rootObjectType;
			RootObjectId = rootObjectId;
			ObjectId = objectId;
			PropName = propName;
			PropValue = propValue;
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
