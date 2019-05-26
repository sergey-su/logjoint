using LogJoint.Postprocessing;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDriver
{
	public interface IReader
	{
		IEnumerableAsync<Message[]> Read(string fileName, Action<double> progressHandler = null);
		IEnumerableAsync<Message[]> Read(Func<Stream> getStream, Action<Stream> releaseStream, Action<double> progressHandler = null);
		bool TestFormat(string logHeader);
	}

	public interface IWriter
	{
		Task Write(Func<Stream> getStream, Action<Stream> releaseStream, IEnumerableAsync<Message[]> messages);
	};
}
