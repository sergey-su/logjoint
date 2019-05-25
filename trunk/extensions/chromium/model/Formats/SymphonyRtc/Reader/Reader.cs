using LogJoint.Postprocessing;
using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using CD = LogJoint.Chromium.ChromeDriver;

namespace LogJoint.Symphony.Rtc
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

		public IEnumerableAsync<Message[]> FromChromeDebugLog(IEnumerableAsync<CDL.Message[]> messages) 
		{
			using (var ctx = new Context())
			{
				return messages.SelectMany(batch => new [] { 
					batch.Select(ctx.FromCDLMessage).Where(m => m != null).ToArray()
				});
			}
		}

		public IEnumerableAsync<Message[]> FromChromeDriverLog(IEnumerableAsync<CD.Message[]> messages) 
		{
			using (var ctx = new Context())
			{
				return messages.SelectMany(batch => new [] { 
					batch.Select(ctx.FromCDMessage).Where(m => m != null).ToArray()
				});
			}
		}

		class Context : IDisposable
		{
			readonly static string mainRegex = @"
(?<d>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3})Z
\ \|\ 
(?<sev>\w+)(\(\d\))?
\ \|\ 
(?<logger>[\w\-\.]+)
\ \|\ 
";

			static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;
				
			readonly Regex logMessageRegex = new Regex("^" + mainRegex, reopts);

			readonly Regex chromeDebugTextRegex = new Regex("^\\\"" + mainRegex + "(?<body>.+)\",\\ source:", reopts);

			void IDisposable.Dispose()
			{
			}

			public Message FromCDLMessage(CDL.Message m) 
			{
				if (m.File != "CONSOLE")
					return null;
				var match = chromeDebugTextRegex.Match(m.Text);
				if (!match.Success)
					return null;
				return new Message(
					m.Index, m.StreamPosition, m.Timestamp,
					new StringSlice(m.Text, match.Groups["sev"]),
					new StringSlice(m.Text, match.Groups["logger"]),
					match.Groups["body"].Value
				);
			}

			public Message FromCDMessage(CD.Message m) 
			{
				return null;
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
									DateTime.ParseExact(headerMatch.Groups["d"].Value, "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFF", CultureInfo.InvariantCulture),
									new StringSlice(mi.Buffer, headerMatch.Groups["sev"]),
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
