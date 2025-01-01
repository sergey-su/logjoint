using System;
using System.Linq;
using System.Collections.Generic;
using A = LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;
using System.Collections.Immutable;

namespace LogJoint.Postprocessing.SequenceDiagram
{
    public class SequenceDiagramVisualizerModel : ISequenceDiagramVisualizerModel
    {
        readonly IManagerInternal postprocessorsManager;
        readonly IUserNamesProvider shortNames;
        readonly ILogSourceNamesProvider logSourceNamesProvider;
        readonly IChangeNotification changeNotification;
        ImmutableHashSet<ISequenceDiagramPostprocessorOutput> outputs = ImmutableHashSet.Create<ISequenceDiagramPostprocessorOutput>();
        ImmutableArray<InternodeMessage> internodeMessages = new ImmutableArray<InternodeMessage>();
        ImmutableArray<Message> unpairedMessages = new ImmutableArray<Message>();
        ImmutableArray<TimelineComment> timelineComments = new ImmutableArray<TimelineComment>();
        ImmutableArray<StateComment> stateComments = new ImmutableArray<StateComment>();
        ImmutableArray<MetadataEntry> metadataEntries = new ImmutableArray<MetadataEntry>();

        public SequenceDiagramVisualizerModel(
            IManagerInternal postprocessorsManager,
            ILogSourcesManager logSourceManager,
            IUserNamesProvider shortNames,
            ILogSourceNamesProvider logSourceNamesProvider,
            IChangeNotification changeNotification)
        {
            this.postprocessorsManager = postprocessorsManager;
            this.shortNames = shortNames;
            this.logSourceNamesProvider = logSourceNamesProvider;
            this.changeNotification = changeNotification;

            postprocessorsManager.Changed += (sender, args) => UpdateOutputs();
            logSourceManager.OnLogSourceTimeOffsetChanged += (s, e) => UpdateCachedContent();
            logSourceManager.OnLogSourceAnnotationChanged += (sender, args) => UpdateCachedContent();
            logSourceManager.OnLogSourceVisiblityChanged += (sender, args) => UpdateOutputs();


            UpdateOutputs();
        }

        IReadOnlyCollection<InternodeMessage> ISequenceDiagramVisualizerModel.InternodeMessages
        {
            get { return internodeMessages; }
        }

        IReadOnlyCollection<Message> ISequenceDiagramVisualizerModel.UnpairedMessages
        {
            get { return unpairedMessages; }
        }

        IReadOnlyCollection<TimelineComment> ISequenceDiagramVisualizerModel.TimelineComments
        {
            get { return timelineComments; }
        }

        IReadOnlyCollection<StateComment> ISequenceDiagramVisualizerModel.StateComments
        {
            get { return stateComments; }
        }

        IReadOnlyCollection<MetadataEntry> ISequenceDiagramVisualizerModel.MetadataEntries
        {
            get { return metadataEntries; }
        }

        IReadOnlyCollection<ISequenceDiagramPostprocessorOutput> ISequenceDiagramVisualizerModel.Outputs
        {
            get { return outputs; }
        }

        void UpdateOutputs()
        {
            var newOutputs = ImmutableHashSet.CreateRange(
                postprocessorsManager.LogSourcePostprocessors
                    .Where(output => output.OutputStatus == LogSourcePostprocessorState.Status.Finished || output.OutputStatus == LogSourcePostprocessorState.Status.Outdated)
                    .Select(output => output.OutputData)
                    .OfType<ISequenceDiagramPostprocessorOutput>()
                    .Where(output => !output.LogSource.IsDisposed)
                    .Where(output => output.LogSource.Visible)
                );
            if (!newOutputs.SetEquals(outputs))
            {
                outputs = newOutputs;
                UpdateCachedContent();
            }
        }

        static A.NodeId MakeNodeId(ISequenceDiagramPostprocessorOutput output)
        {
            return new A.NodeId("role", output.LogSource.ConnectionId);
        }

        class RotatedLogGroup
        {
            public readonly string Key;
            public readonly List<ISequenceDiagramPostprocessorOutput> Outputs;
            public readonly A.Node AnalysisNode;
            public readonly Node Node;

            public RotatedLogGroup(
                IEnumerable<ISequenceDiagramPostprocessorOutput> group,
                IDictionary<ILogSource, LogSourceNames> sourceNames
            )
            {
                this.Outputs = group.ToList();
                this.Outputs.Sort((x, y) => x.RotatedLogPartToken.CompareTo(y.RotatedLogPartToken));
                this.Key = string.Join("#", Outputs.Select(output => output.GetHashCode()));
                this.AnalysisNode = new A.Node(new A.NodeId("role", Key));
                var defaultLogSource = Outputs[0].LogSource;
                var roleName = sourceNames[defaultLogSource];
                this.Node = new Node()
                {
                    Id = AnalysisNode.NodeId.ToString(),
                    TimeOffsets = defaultLogSource.TimeOffsets,
                    RoleInstanceName = roleName.RoleInstanceName,
                    RoleName = roleName.RoleName,
                    LogSources = Outputs.Select(output => output.LogSource).ToList()
                };
            }
        };

        void UpdateCachedContent()
        {
            var messagingEvents = new Dictionary<A.Node, IEnumerable<M.Event>>();
            var eventToLogSource = new Dictionary<M.Event, ILogSource>();
            var nodeInfos = new Dictionary<A.Node, Node>();
            var sourceNames = logSourceNamesProvider.GetSourcesSequenceDiagramNames(
                outputs.Select(output => output.LogSource),
                outputs.Select(output => new
                {
                    LogSource = output.LogSource,
                    SuggestedRoleInstanceName =
                        output.Events
                        .OfType<M.MetadataEvent>()
                        .Where(e => e.Key == M.MetadataKeys.RoleInstanceName)
                        .Select(e => e.Value)
                        .FirstOrDefault(),
                    SuggestedRoleName =
                        output.Events
                        .OfType<M.MetadataEvent>()
                        .Where(e => e.Key == M.MetadataKeys.RoleName)
                        .Select(e => e.Value)
                        .FirstOrDefault()
                })
                .Where(x => x.SuggestedRoleInstanceName != null)
                .ToDictionary(x => x.LogSource, x => new LogSourceNames()
                {
                    RoleInstanceName = shortNames.ResolveShortNamesMurkup(x.SuggestedRoleInstanceName),
                    RoleName = x.SuggestedRoleName
                })
            );
            var groups =
                outputs
                .GroupBy(output => output.RotatedLogPartToken, new PartsOfSameLogEqualityComparer())
                .Select(group => new RotatedLogGroup(group, sourceNames))
                .ToArray();

            Func<A.Message, Message> makeMessageInfo = msg =>
            {
                var node = nodeInfos[msg.Node];
                return new Message()
                {
                    Event = msg.Event,
                    Direction = msg.Direction,
                    LogSource = eventToLogSource[msg.Event],
                    Node = node,
                    Timestamp = node.TimeOffsets.Get(msg.Timestamp)
                };
            };

            foreach (var group in groups)
            {
                messagingEvents.Add(group.AnalysisNode, group.Outputs.SelectMany(x => x.Events));
                nodeInfos.Add(group.AnalysisNode, group.Node);
                foreach (var output in group.Outputs)
                    foreach (var e in output.Events)
                        eventToLogSource[e] = output.LogSource;
            }

            A.IInternodeMessagesDetector internodeMessagesDetector = new A.InternodeMessagesDetector();
            var detectedUnpairedMessages = new List<A.Message>();
            var detectedInternodeMessages = internodeMessagesDetector.DiscoverInternodeMessages(
                messagingEvents, int.MaxValue, detectedUnpairedMessages);

            internodeMessages = ImmutableArray.CreateRange(detectedInternodeMessages.Select(m => new InternodeMessage()
            {
                IncomingMessage = makeMessageInfo(m.IncomingMessage),
                OutgoingMessage = makeMessageInfo(m.OutgoingMessage),
                OutgoingMessageId = m.OutgoingMessage.Key.MessageId,
                OutgoingMessageType = m.OutgoingMessage.Key.Type
            }));

            unpairedMessages = ImmutableArray.CreateRange(detectedUnpairedMessages.Select(makeMessageInfo));

            timelineComments = ImmutableArray.CreateRange(
                from g in groups
                from output in g.Outputs
                from commentEvt in output.TimelineComments
                let commentTime = (commentEvt.Trigger as ITriggerTime)?.Timestamp
                where commentTime != null
                select new TimelineComment()
                {
                    Event = commentEvt,
                    Node = g.Node,
                    Timestamp = g.Node.TimeOffsets.Get(commentTime.Value),
                    LogSource = output.LogSource
                }
            );

            stateComments = ImmutableArray.CreateRange(
                from g in groups
                from output in g.Outputs
                from commentEvt in output.StateComments
                let commentTime = (commentEvt.Trigger as ITriggerTime)?.Timestamp
                where commentTime != null
                select new StateComment()
                {
                    Event = commentEvt,
                    Node = g.Node,
                    Timestamp = g.Node.TimeOffsets.Get(commentTime.Value),
                    LogSource = output.LogSource
                }
            );

            metadataEntries = ImmutableArray.CreateRange(
                from g in groups
                from output in g.Outputs
                from metaEvent in output.Events.OfType<M.MetadataEvent>()
                select new MetadataEntry()
                {
                    Event = metaEvent,
                    Node = g.Node,
                    LogSource = output.LogSource
                }
            );

            changeNotification.Post();
        }
    };
}
