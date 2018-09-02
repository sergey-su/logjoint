using LogJoint.Analytics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Chromium.HttpArchive
{
	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(string fileName, string logFileNameHint = null, Action<double> progressHandler = null);
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, string logFileNameHint = null, Action<double> progressHandler = null);
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
		public StringSlice ObjectType;
		public StringSlice ObjectId;
		public StringSlice Severity;
		public StringSlice MessageType;
		public readonly string Text;

		int IOrderedTrigger.Index { get { return Index; } }

		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

		string ITriggerText.Text { get { return Text; } }

		// severities
		public const string INFO = "I";
		public const string WARN = "W";

		// object types
		public const string ENTRY = "entry";

		public const int MsgTypeLength = 7;
		// stages types
		public const string START   = "start  ";
		public const string END     = "end    ";
		public const string BLOCKED = "blocked";
		public const string DNS     = "dns    ";
		public const string CONNECT = "connect";
		public const string SEND    = "send   ";
		public const string WAIT    = "wait   ";
		public const string RECEIVE = "receive";
		public const string SSL     = "ssl    ";
		// attribute types
		public const string HEADER  = "header ";
		public const string BODY    = "body   ";
		public const string META    = "meta   ";

		public Message(
			int index, 
			long position,
			DateTime ts,
			StringSlice objType,
			StringSlice objId,
			StringSlice messageType,
			StringSlice severity,
			string text
		)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			ObjectId = objId;
			ObjectType = objType;
			MessageType = messageType;
			Severity = severity;
			Text = text;
		}
	};
}
