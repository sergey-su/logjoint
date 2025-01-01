using System;
using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using SI = LogJoint.Postprocessing.StateInspector;
using System.Diagnostics;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.SequenceDiagram
{
    public interface ISequenceDiagramPostprocessorOutput : IPostprocessorOutputETag
    {
        ILogSource LogSource { get; }
        IEnumerable<M.Event> Events { get; }
        IEnumerable<TL.Event> TimelineComments { get; }
        IEnumerable<SI.Event> StateComments { get; }
        ILogPartToken RotatedLogPartToken { get; }
    };

    public interface ISequenceDiagramVisualizerModel
    {
        IReadOnlyCollection<InternodeMessage> InternodeMessages { get; }
        IReadOnlyCollection<Message> UnpairedMessages { get; }
        IReadOnlyCollection<TimelineComment> TimelineComments { get; }
        IReadOnlyCollection<StateComment> StateComments { get; }
        IReadOnlyCollection<MetadataEntry> MetadataEntries { get; }
        IReadOnlyCollection<ISequenceDiagramPostprocessorOutput> Outputs { get; }
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
