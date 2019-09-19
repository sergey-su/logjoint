using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class Reader : IReader
	{
		readonly ITextLogParser textLogParser;
		readonly CancellationToken cancellation;

		public Reader(ITextLogParser textLogParser, CancellationToken cancellation)
		{
			this.textLogParser = textLogParser;
			this.cancellation = cancellation;
		}

		public IEnumerableAsync<Message[]> Read(string dataFileName, Action<double> progressHandler)
		{
			return Read(() => new FileStream(dataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), s => s.Dispose(), progressHandler);
		}

		public IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, cancellation, progressHandler, textLogParser), false);
		}

		public static Message Read(string line)
		{
			using (var ctx = new Context())
				return ctx.Read(line);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

			readonly Regex logMessageRegex = new Regex(@"^(?<date>\d{4}-\d{2}-\d{2}\ \d{2}\:\d{2}\:\d{2}\.\d{3})\s+
(?<sev>ERROR|WARN|\w+)\s+
(?<pid>\d+)\s+
\-\-\-\s+
\[(?<tid>[^\]]+)\]\s*
(?<logger>\S+)\s*
\:\s*", regexOptions | RegexOptions.Multiline);

			void IDisposable.Dispose()
			{
			}

			public async Task Read(
				IYieldAsync<Message[]> yieldAsync,
				Func<Stream> getStream, Action<Stream> releaseStream,
				CancellationToken cancellation,
				Action<double> progressHandler,
				ITextLogParser textLogParser)
			{
				var inputStream = getStream();
				try
				{
					await textLogParser.ParseStream(
						inputStream,
						textLogParser.CreateRegexHeaderMatcher(logMessageRegex),
						async messagesInfo =>
						{
							var outMessages = new Message[messagesInfo.Count];
							for (int i = 0; i < messagesInfo.Count; ++i)
							{
								var mi = messagesInfo[i];
								var headerMatch = ((IRegexHeaderMatch)mi.HeaderMatch).Match;
								var body = mi.MessageBoby;
								outMessages[i] = MakeMessage(
									mi.MessageIndex,
									mi.StreamPosition,
									headerMatch,
									mi.Buffer,
									body
								);
							}

							if (cancellation.IsCancellationRequested)
								return false;

							return await yieldAsync.YieldAsync(outMessages.ToArray());
						}, new TextLogParserOptions(progressHandler));
				}
				finally
				{
					releaseStream(inputStream);
				}
			}

			public Message Read(string line)
			{
				var m = logMessageRegex.Match(line);
				if (!m.Success)
					return null;
				return MakeMessage(0, 0, m, line, line.Substring(m.Index + m.Length));
			}

			Message MakeMessage(int messageIndex, long streamPosition, Match headerMatch, string headerBuffer, string body)
			{
				return new Message(
					messageIndex,
					streamPosition,
					DateTime.ParseExact(headerMatch.Groups["date"].Value, "yyyy'-'MM'-'dd' 'HH':'mm':'ss.FFF", CultureInfo.InvariantCulture),
					new StringSlice(headerBuffer, headerMatch.Groups["sev"]),
					new StringSlice(headerBuffer, headerMatch.Groups["pid"]),
					new StringSlice(headerBuffer, headerMatch.Groups["tid"]),
					new StringSlice(headerBuffer, headerMatch.Groups["logger"]),
					body
				);
			}
		}
	}
}
