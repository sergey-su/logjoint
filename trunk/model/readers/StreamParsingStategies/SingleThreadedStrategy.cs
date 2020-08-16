using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LogJoint.RegularExpressions;

namespace LogJoint.StreamParsingStrategies
{
	public abstract class SingleThreadedStrategy : BaseStrategy
	{
		public SingleThreadedStrategy(ILogMedia media, Encoding encoding, IRegex headerRe, MessagesSplitterFlags splitterFlags, TextStreamPositioningParams textStreamPositioningParams,
				ITraceSourceFactory traceSourceFactory = null)
			: base(media, encoding, headerRe, textStreamPositioningParams)
		{
			this.trace = traceSourceFactory?.CreateTraceSource("App", "strat") ?? LJTraceSource.EmptyTracer;
			this.textSplitter = new ReadMessageFromTheMiddleProblem(new MessagesSplitter(new StreamTextAccess(media.DataStream, encoding, textStreamPositioningParams), headerRe, splitterFlags));
		}

		public override async Task ParserCreated(CreateParserParams p)
		{
			postprocessor = p.PostprocessorsFactory?.Invoke();
			await textSplitter.BeginSplittingSession(p.Range.Value, p.StartPosition, p.Direction);

			// todo
			//if (textSplitter.CurrentMessageIsEmpty)
			//{
			//    if (direction == MessagesParserDirection.Forward)
			//    {
			//        if ((initParams.startPosition == reader.BeginPosition)
			//         || ((reader.EndPosition - initParams.startPosition) >= StreamTextAccess.MaxTextBufferSize))
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
			postprocessor?.Dispose();
		}

		public override async ValueTask<IMessage> ReadNext()
		{
			using (var perfop = new Profiling.Operation(trace, "ReadNext"))
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				if (!await textSplitter.GetCurrentMessageAndMoveToNextOne(capture))
					return null;
				perfop.Milestone("got capture in " + stopwatch.Elapsed.TotalMilliseconds.ToString());
				stopwatch.Restart();
				var msg = MakeMessage(capture);
				perfop.Milestone("made message in " + stopwatch.Elapsed.TotalMilliseconds.ToString());
				return msg;
			}
		}

		public override async ValueTask<PostprocessedMessage> ReadNextAndPostprocess() 
		{
			var msg = await ReadNext();
			return new PostprocessedMessage(
				msg, 
				msg != null ? postprocessor?.Postprocess(msg) : null
			);
		}

		protected abstract IMessage MakeMessage(TextMessageCapture capture);

		IMessagesSplitter textSplitter;
		TextMessageCapture capture = new TextMessageCapture();
		IMessagesPostprocessor postprocessor;
		LJTraceSource trace;
	}
}
