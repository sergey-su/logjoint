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
		readonly CancellationToken cancellation;

		public Reader(CancellationToken cancellation)
		{
			this.cancellation = cancellation;
		}

		public Reader()
			: this(CancellationToken.None)
		{
		}

		public IEnumerableAsync<Message[]> Read(string dataFileName, Action<double> progressHandler = null)
		{
			return Read(() => new FileStream(dataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), s => s.Dispose(), progressHandler);
		}

		public IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, cancellation, progressHandler), false);
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
				Action<double> progressHandler)
			{
				var inputStream = getStream();
				try
				{
					await TextLogParser.ParseStream(
						inputStream,
						new RegexHeaderMatcher(logMessageRegex),
						async messagesInfo =>
						{
							var outMessages = new Message[messagesInfo.Count];
							for (int i = 0; i < messagesInfo.Count; ++i)
							{
								var mi = messagesInfo[i];
								var headerMatch = ((RegexHeaderMatch)mi.HeaderMatch).Match;
								var body = mi.MessageBoby;
								outMessages[i] = new Message(
									mi.MessageIndex,
									mi.StreamPosition,
									DateTime.ParseExact(headerMatch.Groups["date"].Value, "yyyy'-'MM'-'dd' 'HH':'mm':'ss.FFF", CultureInfo.InvariantCulture),
									new StringSlice(mi.Buffer, headerMatch.Groups["sev"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["pid"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["tid"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["logger"]),
									body
								);
							}

							if (cancellation.IsCancellationRequested)
								return false;

							return await yieldAsync.YieldAsync(outMessages.ToArray());
						}, progressHandler);
				}
				finally
				{
					releaseStream(inputStream);
				}
			}
		}
	}
}
