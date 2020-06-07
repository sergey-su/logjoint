using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Google.Analog
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
			return Read(() => new FileStream(dataFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), s => s.Dispose(), progressHandler);
		}

		public IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, cancellation, progressHandler, textLogParser), false);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

			readonly Regex logMessageRegex = new Regex(
				@"^(?<sev>[IWE])\ (?<ts>\d{4}\ [\d\:]{8}\.\d{6})\s*(?<th>\d+)\s(?<thn>\S+)\s(?<fn>[^\:]+)\:(?<ln>\d+)\]\ ",
				regexOptions | RegexOptions.Multiline);

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
								outMessages[i] = new Message(
									mi.MessageIndex,
									mi.StreamPosition,
									new StringSlice(mi.Buffer, headerMatch.Groups["th"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["thn"]),
									DateTime.ParseExact(headerMatch.Groups["ts"].Value, @"MMdd HH\:mm\:ss\.ffffff", CultureInfo.InvariantCulture),
									new StringSlice(mi.Buffer, headerMatch.Groups["sev"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["fn"]),
									new StringSlice(mi.Buffer, headerMatch.Groups["ln"]),
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
