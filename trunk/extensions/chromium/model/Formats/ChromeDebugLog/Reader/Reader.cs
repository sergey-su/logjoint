using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDebugLog
{
	public class Reader : IReader
	{
		readonly CancellationToken cancellation;
		readonly ITextLogParser textLogParser;

		public Reader(ITextLogParser textLogParser, CancellationToken cancellation)
		{
			this.cancellation = cancellation;
			this.textLogParser = textLogParser;
		}

		public IEnumerableAsync<Message[]> Read(string dataFileName, Action<double> progressHandler = null)
		{
			return Read(() => Task.FromResult<Stream>(new FileStream(dataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), s => s.Dispose(), progressHandler);
		}

		public IEnumerableAsync<Message[]> Read(Func<Task<Stream>> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, cancellation, progressHandler, textLogParser), false);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

			readonly Regex logMessageRegex = new Regex(@"
^\[
((?<pid>\d+)\:(?<tid>\d+)\:)?
(?<time>\d{4}\/\d{6}\.\d{3,6})\:
(?<sev>\w+)\:
(?<file>[\w\\\/\.]*)
\((?<line>\d+)\)
\]\ ", regexOptions | RegexOptions.Multiline);

			void IDisposable.Dispose()
			{
			}

			public async Task Read(
				IYieldAsync<Message[]> yieldAsync,
				Func<Task<Stream>> getStream, Action<Stream> releaseStream,
				CancellationToken cancellation,
				Action<double> progressHandler,
				ITextLogParser textLogParser)
			{
				var inputStream = await getStream();
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
								outMessages[i] = new Message(
									mi.MessageIndex,
									mi.StreamPosition,
									new StringSlice(mi.Buffer, headerMatch.Groups["pid"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["tid"]),
									DateTime.ParseExact(headerMatch.Groups["time"].Value, "MMdd/HHmmss.FFFFFF", CultureInfo.InvariantCulture),
									new StringSlice(mi.Buffer, headerMatch.Groups["sev"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["file"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["line"]),
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
		}
	}
}
