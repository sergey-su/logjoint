using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.RegularExpressions;

namespace LogJoint.StreamParsingStrategies
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

		public virtual Task ParserCreated(CreateParserParams p) { return Task.CompletedTask; }
		public virtual void ParserDestroyed() { }
		public virtual ValueTask<IMessage> ReadNext() { return new ValueTask<IMessage>((IMessage)null); }
		public virtual async ValueTask<PostprocessedMessage> ReadNextAndPostprocess() { return new PostprocessedMessage(await ReadNext(), null); }

		protected readonly ILogMedia media;
		protected readonly Encoding encoding;
		protected readonly IRegex headerRe;
		protected readonly TextStreamPositioningParams textStreamPositioningParams;
	}
}
