using System;
using System.Text;
using System.Threading.Tasks;
using LogJoint.RegularExpressions;

namespace LogJoint.StreamReadingStrategies
{
    public class BaseStrategy
    {
        public BaseStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, TextStreamPositioningParams textStreamPositioningParams)
        {
            this.media = media;
            this.encoding = encoding;
            this.headerRe = headerRe;
            this.textStreamPositioningParams = textStreamPositioningParams;
        }

        public virtual Task ParserCreated(ReadMessagesParams p) { return Task.CompletedTask; }
        public virtual void ParserDestroyed() { }
        public virtual ValueTask<PostprocessedMessage> ReadNextAndPostprocess() { return ValueTask.FromResult(new PostprocessedMessage(null, null)); }

        protected readonly ILogMedia media;
        protected readonly Encoding encoding;
        protected readonly IRegex headerRe;
        protected readonly TextStreamPositioningParams textStreamPositioningParams;
    }
}
