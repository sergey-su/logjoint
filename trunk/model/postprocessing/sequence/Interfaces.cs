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
        public required Message OutgoingMessage;
        public required Message IncomingMessage;
        public required string OutgoingMessageId;
        public M.MessageType OutgoingMessageType;
    };

    public class Message
    {
        public required Node Node;
        public DateTime Timestamp;
        public required M.Event Event;
        public M.MessageDirection Direction;
        public required ILogSource LogSource;
    };

    public class TimelineComment
    {
        public required Node Node;
        public DateTime Timestamp;
        public required TL.Event Event;
        public required ILogSource LogSource;
    };

    public class StateComment
    {
        public required Node Node;
        public DateTime Timestamp;
        public required SI.Event Event;
        public required ILogSource LogSource;
    };

    [DebuggerDisplay("{Event}")]
    public class MetadataEntry
    {
        public required Node Node;
        public required M.MetadataEvent Event;
        public required ILogSource LogSource;
    };

    public class Node
    {
        public required string Id;
        public required ITimeOffsets TimeOffsets;
        public required IList<ILogSource> LogSources;
        public required string RoleInstanceName;
        public required string RoleName;
    };
}
