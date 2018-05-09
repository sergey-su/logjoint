using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;

namespace LogJoint.Chromium.ChromeDriver
{
	namespace DevTools.Events
	{
		namespace Network
		{
			public class Base
			{
				public readonly static string Prefix = "DEVTOOLS EVENT Network.";

				public string requestId;
				public string frameId;

				public static string ParseRequestPid(string requestId)
				{
					var a = requestId.Split('.');
					return a.Length == 2 ? a[0] : null;
				}
			};

			public class RequestWillBeSent: Base
			{
				public readonly static string EventType = "requestWillBeSent";

				public Request request;

				public class Request
				{
					public string method;
					public string url;
				};
			};

			public class ResponseReceived: Base
			{
				public readonly static string EventType = "responseReceived";

				public Response response;

				public class Response
				{
					public Timing timing;

					public class Timing
					{
						public double? requestTime;
						public double? dnsStart;
						public double? dnsEnd;
						public double? proxyStart;
						public double? proxyEnd;
						public double? pushStart;
						public double? pushEnd;
						public double? sendStart;
						public double? sendEnd;
						public double? sslStart;
						public double? sslEnd;
						public double? receiveHeadersEnd;
					};
				};
			};


			public class LoadingFinished: Base
			{
				public readonly static string EventType = "loadingFinished";
			};

			public class DataReceived: Base
			{
				public readonly static string EventType = "dataReceived";

				public double? dataLength;
				public double? encodedDataLength;
			};

			public class RequestServedFromCache: Base
			{
				public readonly static string EventType = "requestServedFromCache";
			};

			public class WebSocketBase: Base
			{
				public readonly static new string Prefix = Base.Prefix + "webSocket";
			};

			public class WebSocketCreated: WebSocketBase
			{
				public readonly static string EventType = "webSocketCreated";

				public string url;
			};

			public class WebSocketClosed: WebSocketBase
			{
				public readonly static string EventType = "webSocketClosed";
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

		public class TimeStampsInfo
		{
			public double? timestamp;
			public double? wallTime;
			public string requestId;
			public Network.ResponseReceived.Response response;
			public string type;
		}

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
