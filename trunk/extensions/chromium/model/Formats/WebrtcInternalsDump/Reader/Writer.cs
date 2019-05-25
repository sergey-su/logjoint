using LogJoint.Postprocessing;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Chromium.WebrtcInternalsDump
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
								"{0:yyyy-MM-ddTHH:mm:ss.ffffff}|{1}|{2}|{3}|{4}|{5}\n",
								m.Timestamp,
								m.RootObjectType.ToString(),
								m.RootObjectId.ToString(),
								m.ObjectId.ToString(),
								m.PropName.ToString(),
								m.PropValue.ToString()
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
