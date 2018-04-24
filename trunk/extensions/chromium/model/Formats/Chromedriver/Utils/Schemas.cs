using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;

namespace LogJoint.Chromium.ChromeDriver
{
	namespace DevTools.Events
	{
		namespace Network
		{
			public class Generic
			{
				public readonly static string Prefix = "DEVTOOLS EVENT Network.";

				public string requestId;
				public Request request;
				public string frameId;

				public class Request
				{
					public string method;
					public string url;
				};
			};
		}

		namespace Tracing
		{
			public class DataCollected 
			{
				public readonly static string Prefix = "DEVTOOLS EVENT Tracing.dataCollected";

				public Entry[] value;

				public class Entry
				{
					public uint? pid;
					public uint? tid;
				};
			};
		};

		namespace Runtime
		{
			public class LogAPICalled
			{
				public readonly static string Prefix = "DEVTOOLS EVENT Runtime.consoleAPICalled";

				public Arg[] args;

				public class Arg
				{
					public string type;
					public object value;
				};
			};
		};

		public class LogMessage
		{
			readonly static Regex regex = new Regex(@"^DEVTOOLS EVENT (?<ns>\w+)\.(?<evt>\w+) (?<payload>.*)$",
				RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

			public string EventNamespace { get; private set; }
			public string EventType { get; private set; }
			public string Payload { get; private set; }

			public T ParsePayload<T>()
			{
				try
				{
					return JsonConvert.DeserializeObject<T>(Payload);
				}
				catch (Exception)
				{
					return default(T);
				}
			}

			public static LogMessage Parse(string str)
			{
				var m = regex.Match(str);
				if (!m.Success)
					return null;
				return new LogMessage()
				{
					EventNamespace = m.Groups["ns"].Value,
					EventType = m.Groups["evt"].Value,
					Payload = m.Groups["payload"].Value,
				};
			}
		}
	};
}
