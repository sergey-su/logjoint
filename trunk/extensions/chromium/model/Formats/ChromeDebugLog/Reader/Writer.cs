using LogJoint.Analytics;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDebugLog
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
							await streamWriter.WriteAsync(string.Format(
								"[{0}:{1}:{2:MMdd\\/HHmmss.fff}:{3}:{4}({5})] {6}\n",
								m.ProcessId,
								m.ThreadId,
								m.Timestamp,
								m.Severity,
								m.File,
								m.LineNum,
								m.Text
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
