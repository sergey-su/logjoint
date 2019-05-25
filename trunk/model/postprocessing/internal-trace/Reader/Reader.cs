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

		public Reader(CancellationToken cancellation)
		{
			this.cancellation = cancellation;
		}

		public Reader(): this(CancellationToken.None)
		{ }


		public IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, string fileNameHint = null, Action<double> progressHandler = null)
		{
			using (var ctx = new Context())
				return EnumerableAsync.Produce<Message[]>(yieldAsync => ctx.Read(yieldAsync, getStream, releaseStream, fileNameHint, cancellation, progressHandler), false);
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

			public async Task Read(IYieldAsync<Message[]> yieldAsync, Func<Stream> getStream, Action<Stream> releaseStream, string fileNameHint, 
				CancellationToken cancellation, Action<double> progressHandler)
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
