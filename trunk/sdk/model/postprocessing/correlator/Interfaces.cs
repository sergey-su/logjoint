using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
    public interface ISameNodeDetectionToken
    {
        SameNodeDetectionResult DetectSameNode(ISameNodeDetectionToken otherNodeToken);
        ISameNodeDetectionTokenFactory Factory { get; }
        void Serialize(XElement node);
    };

    public class SameNodeDetectionResult
    {
        public TimeSpan TimeDiff { get; private set; }
        public SameNodeDetectionResult(TimeSpan timeDiff)
        {
            TimeDiff = timeDiff;
        }
    };


    public interface ISameNodeDetectionTokenFactory
    {
        /// <summary>
        /// Permanent unique ID of this factory.
        /// It's stored in persistent storage. It's used to find the
        /// factory that can deserialize the stored tokens.
        /// </summary>
        string Id { get; }
        ISameNodeDetectionToken Deserialize(XElement element);
    };

    public class PostprocessorOutputBuilder
    {
        public PostprocessorOutputBuilder SetLogPartToken(Task<ILogPartToken> value) { logPart = value; return this; }
        public PostprocessorOutputBuilder SetMessagingEvents(IEnumerableAsync<M.Event[]> value) { events = value; return this; }
        public PostprocessorOutputBuilder SetSameNodeDetectionToken(Task<ISameNodeDetectionToken> value) { sameNodeDetectionToken = value; return this; }
        public PostprocessorOutputBuilder SetTriggersConverter(Func<object, TextLogEventTrigger> value) { triggersConverter = value; return this; }
        public Task Build(LogSourcePostprocessorInput postprocessorParams) { return build(postprocessorParams, this); }

        internal IEnumerableAsync<M.Event[]> events;
        internal Task<ILogPartToken> logPart;
        internal Task<ISameNodeDetectionToken> sameNodeDetectionToken;
        internal Func<object, TextLogEventTrigger> triggersConverter;
        internal Func<LogSourcePostprocessorInput, PostprocessorOutputBuilder, Task> build;
    };

    public interface IModel
    {
        PostprocessorOutputBuilder CreatePostprocessorOutputBuilder();
    };
}
