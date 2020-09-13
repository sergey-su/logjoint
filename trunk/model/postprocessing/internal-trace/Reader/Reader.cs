using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.InternalTrace
{
	public class Reader : IReader
	{
		readonly CancellationToken cancellation;
		readonly ITextLogParser textLogParser;

		public Reader(CancellationToken cancellation, ITextLogParser textLogParser)
		{
			this.cancellation = cancellation;
			this.textLogParser = textLogParser;
		}

		public Reader(ITextLogParser textLogParser) : this(CancellationToken.None, textLogParser)
		{ }


		public IEnumerableAsync<Message[]> Read(Func<Task<Stream>> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, textLogParser, cancellation, progressHandler), false);
		}

		class Context : IDisposable
		{
			const RegexOptions regexOptions =
				RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;

			readonly Regex logMessageRegex = new Regex(@"
^
(?<d>[\d\/]{10}\ [\d\:\.]{12})\ 
(?<th>T\#\d+)\ 
(?<s>[CEWIV\{\}SRT\?])\ 
((?<src>[\w\.]+)\:\ )?
",				regexOptions
			);

			void IDisposable.Dispose()
			{
			}

			public async Task Read(IYieldAsync<Message[]> yieldAsync, Func<Task<Stream>> getStream, Action<Stream> releaseStream, ITextLogParser textLogParser, 
				CancellationToken cancellation, Action<double> progressHandler)
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
							var headerMatch = ((RegexHeaderMatch)mi.HeaderMatch).Match;
							var body = mi.MessageBoby;
							outMessages[i] = new Message(
								mi.MessageIndex,
								mi.StreamPosition,
								DateTime.ParseExact(headerMatch.Groups["d"].Value, "yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
								new StringSlice(mi.Buffer, headerMatch.Groups["th"]),
								new StringSlice(mi.Buffer, headerMatch.Groups["s"]),
								new StringSlice(mi.Buffer, headerMatch.Groups["src"]),
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
