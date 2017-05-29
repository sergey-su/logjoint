using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Analytics.InternalTrace
{
	public static class Extensions
	{
		public static IEnumerableAsync<Message[]> Read(this IReader reader, string fileName, string originalFileNameHint = null, Action<double> progressHandler = null)
		{
			return reader.Read(() => new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), s => s.Dispose(), originalFileNameHint ?? fileName, progressHandler);
		}

		public static Task Write(this IWriter writer, string fileName, IEnumerableAsync<Message[]> messages)
		{
			return writer.Write(() => new FileStream(fileName, FileMode.Create), s => s.Dispose(), messages);
		}
	}
}
