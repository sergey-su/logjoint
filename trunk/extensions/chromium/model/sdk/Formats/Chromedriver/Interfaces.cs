using LogJoint.Postprocessing;
using System;
using System.Diagnostics;

namespace LogJoint.Chromium.ChromeDriver
{
	[DebuggerDisplay("{Text}")]
	public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition, ITriggerText
	{
		public readonly int Index;
		public readonly long StreamPosition;
		public readonly DateTime Timestamp;
		public StringSlice Severity;
		public readonly string Text;
		public char MillisSeparator;

		int IOrderedTrigger.Index { get { return Index; } }

		DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

		long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

		string ITriggerText.Text { get { return Text; } }

		public Message(
			int index,
			long position,
			DateTime ts,
			char millisSeparator,
			StringSlice severity,
			string text
		)
		{
			Index = index;
			StreamPosition = position;
			Timestamp = ts;
			MillisSeparator = millisSeparator;
			Severity = severity;
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
