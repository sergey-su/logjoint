using LogJoint.Analytics;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDriver
{
	public class Writer : IWriter
	{
		public async Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages)
		{
			var stream = getStream();
			try
			{
				using (var streamWriter = new StreamWriter(stream, Encoding.ASCII, 32 * 1024, true))
					await messages.ForEach(async batch =>
					{
						foreach (var m in batch)
						{
							var ts = TimeUtils.ToUnixTimestampMillis(m.Timestamp);
							await streamWriter.WriteAsync(string.Format(
								"[{0}{4}{1:D3}][{2}]: {3}\n",
								ts/1000,
								ts%1000,
								m.Severity,
								m.Text,
								m.MillisSeparator
							));
						}
						return true;
					});
			}
			finally
			{
				releaseStream(stream);
			}
		}
	}
}
