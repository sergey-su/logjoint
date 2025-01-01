using LogJoint.Postprocessing;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wireshark.Dpml
{
    public interface IReader
    {
        IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null);
    }

    public interface IWriter
    {
        Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages);
    };

    public class Message : IOrderedTrigger, ITriggerTime, ITriggerStreamPosition
    {
        public readonly int Index;
        public readonly long StreamPosition;
        public readonly long FrameNum;
        public readonly DateTime Timestamp;
        public readonly ImmutableDictionary<string, Proto> Protos;

        int IOrderedTrigger.Index { get { return Index; } }

        DateTime ITriggerTime.Timestamp { get { return Timestamp; } }

        long ITriggerStreamPosition.StreamPosition { get { return StreamPosition; } }

        [DebuggerDisplay("{DisplayName}")]
        public class Proto
        {
            public readonly string DisplayName;
            public readonly int Index;
            public readonly ImmutableDictionary<string, Field> Fields;

            public Proto(string displayName, int index, ImmutableDictionary<string, Field> fields)
            {
                this.DisplayName = displayName;
                this.Fields = fields;
                this.Index = index;
            }
        };

        public struct Field
        {
            public readonly string Show;
            public readonly string Value;

            public Field(string show, string value)
            {
                this.Show = show;
                this.Value = value;
            }

            public override string ToString()
            {
                return $"{Show} {Value}";
            }
        };

        public Message(
            int index,
            long position,
            DateTime ts,
            long frameNum,
            ImmutableDictionary<string, Proto> protos
        )
        {
            Index = index;
            StreamPosition = position;
            Timestamp = ts;
            FrameNum = frameNum;
            Protos = protos;
        }

        public override string ToString()
        {
            return Protos.Values.MaxByKey(p => p.Index)?.DisplayName ?? "<no proto>";
        }
    };
}
