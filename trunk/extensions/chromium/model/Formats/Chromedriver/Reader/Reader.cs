using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDriver
{
	public class Reader : IReader
	{
		CancellationToken cancellation;

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

		public bool TestFormat(string logHeader)
		{
			using (var ctx = new Context())
				return ctx.Test(logHeader);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

			readonly Regex logMessageRegex = new Regex(@"
^\[(?<d>\d+)(?<mils>[\.\,])(?<ms>\d+)\]
\[(?<sev>\w+)\]\:\ ", regexOptions | RegexOptions.Multiline);

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
									TimeUtils.UnixTimestampMillisToDateTime(
										long.Parse(headerMatch.Groups["d"].Value, CultureInfo.InvariantCulture) * 1000 +
										long.Parse(headerMatch.Groups["ms"].Value, CultureInfo.InvariantCulture)
									).ToUnspecifiedTime(),
									headerMatch.Groups["mils"].Value[0],
									new StringSlice(mi.Buffer, headerMatch.Groups["sev"]),
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

			public bool Test(string logHeader)
			{
				return logMessageRegex.IsMatch(logHeader);
			}
		}
	}
}
