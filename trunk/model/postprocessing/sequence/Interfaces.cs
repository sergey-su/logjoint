using System;
using System.Collections.Generic;
using M = LogJoint.Analytics.Messaging;
using TL = LogJoint.Analytics.Timeline;
using SI = LogJoint.Analytics.StateInspector;
using System.Diagnostics;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	public interface ISequenceDiagramPostprocessorOutput
	{
		ILogSource LogSource { get; }
		IEnumerable<M.Event> Events { get; }
		IEnumerable<TL.Event> TimelineComments { get; }
		IEnumerable<SI.Event> StateComments { get; }
		ILogPartToken RotatedLogPartToken { get; }
	};

	public interface ISequenceDiagramVisualizerModel
	{
		IEnumerable<InternodeMessage> InternodeMessages { get; }
		IEnumerable<Message> UnpairedMessages { get; }
		IEnumerable<TimelineComment> TimelineComments { get; }
		IEnumerable<StateComment> StateComments { get; }
		IEnumerable<MetadataEntry> MetadataEntries { get; }
		IEnumerable<ISequenceDiagramPostprocessorOutput> Outputs { get; }

		event EventHandler Changed;
	};

	public class InternodeMessage
	{
		public Message OutgoingMessage;
		public Message IncomingMessage;
		public string OutgoingMessageId;
		public M.MessageType OutgoingMessageType;
	};

	public class Message
	{
		public Node Node;
		public DateTime Timestamp;
		public M.Event Event;
		public M.MessageDirection Direction;
		public ILogSource LogSource;
	};

	public class TimelineComment
	{
		public Node Node;
		public DateTime Timestamp;
		public TL.Event Event;
		public ILogSource LogSource;
	};

	public class StateComment
	{
		public Node Node;
		public DateTime Timestamp;
		public SI.Event Event;
		public ILogSource LogSource;
	};

	[DebuggerDisplay("{Event}")]
	public class MetadataEntry
	{
		public Node Node;
		public M.MetadataEvent Event;
		public ILogSource LogSource;
	};

	public class Node
	{
		public string Id;
		public ITimeOffsets TimeOffsets;
		public IList<ILogSource> LogSources;
		public string RoleInstanceName;
		public string RoleName;
	};
}
