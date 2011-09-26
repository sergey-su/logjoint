using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.RegularExpressions;

namespace LogJoint.StreamParsingStrategies
{
	public abstract class SingleThreadedStrategy : BaseStrategy
	{
		public SingleThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, MessagesSplitterFlags splitterFlags)
			: base(media, encoding, headerRe)
		{
			this.textSplitter = new ReadMessageFromTheMiddleProblem(new MessagesSplitter(new StreamTextAccess(media.DataStream, encoding), headerRe, splitterFlags));
		}

		public override void ParserCreated(CreateParserParams p)
		{
			textSplitter.BeginSplittingSession(p.Range.Value, p.StartPosition, p.Direction);

			// todo
			//if (textSplitter.CurrentMessageIsEmpty)
			//{
			//    if (direction == MessagesParserDirection.Forward)
			//    {
			//        if ((p.startPosition == reader.BeginPosition)
			//         || ((reader.EndPosition - p.startPosition) >= StreamTextAccess.MaxTextBufferSize))
			//        {
			//            throw new InvalidFormatException();
			//        }
			//    }
			//    else
			//    {
			//        // todo
			//    }
			//}
		}

		public override void ParserDestroyed()
		{
			textSplitter.EndSplittingSession();
		}

		public override MessageBase ReadNext()
		{
			if (!textSplitter.GetCurrentMessageAndMoveToNextOne(capture))
				return null;
			return MakeMessage(capture);
		}

		protected abstract MessageBase MakeMessage(TextMessageCapture capture);

		IMessagesSplitter textSplitter;
		TextMessageCapture capture = new TextMessageCapture();
	}
}
