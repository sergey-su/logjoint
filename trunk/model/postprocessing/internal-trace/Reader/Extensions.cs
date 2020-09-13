using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.InternalTrace
{
	public static class Extensions
	{
		public static IEnumerableAsync<Message[]> Read(this IReader reader, Func<Task<Stream>> openStream, Action<double> progressHandler = null)
		{
			return reader.Read(openStream, s => s.Dispose(), progressHandler);
		}

		public static Task Write(this IWriter writer, string fileName, IEnumerableAsync<Message[]> messages)
		{
			return writer.Write(() => new FileStream(fileName, FileMode.Create), s => s.Dispose(), messages);
		}
	}
}
