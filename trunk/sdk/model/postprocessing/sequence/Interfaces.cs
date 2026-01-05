using System;
using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using SI = LogJoint.Postprocessing.StateInspector;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.SequenceDiagram
{
    public class PostprocessorOutputBuilder
    {
        public PostprocessorOutputBuilder SetLogPartToken(Task<ILogPartToken> value) { logPart = value; return this; }
        public PostprocessorOutputBuilder SetMessagingEvents(IEnumerableAsync<M.Event[]> value) { events = value; return this; }
        public PostprocessorOutputBuilder SetTimelineComments(IEnumerableAsync<TL.Event[]> value) { timelineComments = value; return this; }
        public PostprocessorOutputBuilder SetStateInspectorComments(IEnumerableAsync<SI.Event[]> value) { stateInspectorComments = value; return this; }
        public PostprocessorOutputBuilder SetTriggersConverter(Func<object, TextLogEventTrigger> value) { triggersConverter = value; return this; }
        public Task Build(LogSourcePostprocessorInput postprocessorParams) { return build(postprocessorParams, this); }

        internal IEnumerableAsync<M.Event[]>? events;
        internal IEnumerableAsync<TL.Event[]>? timelineComments;
        internal IEnumerableAsync<SI.Event[]>? stateInspectorComments;
        internal Task<ILogPartToken>? logPart;
        internal Func<object, TextLogEventTrigger>? triggersConverter;
        private Func<LogSourcePostprocessorInput, PostprocessorOutputBuilder, Task> build;

        internal PostprocessorOutputBuilder(Func<LogSourcePostprocessorInput, PostprocessorOutputBuilder, Task> build) => this.build = build;
    };

    public interface IModel
    {
        PostprocessorOutputBuilder CreatePostprocessorOutputBuilder();
        [Obsolete]
        Task SavePostprocessorOutput(
            IEnumerableAsync<M.Event[]> events,
            IEnumerableAsync<TL.Event[]> timelineComments,
            IEnumerableAsync<SI.Event[]> stateInspectorComments,
            Task<ILogPartToken> logPartToken,
            Func<object, TextLogEventTrigger> triggersConverter,
            LogSourcePostprocessorInput postprocessorInput
        );
    };
}
