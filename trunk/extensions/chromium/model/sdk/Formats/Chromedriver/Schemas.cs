using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;
using System.IO;

namespace LogJoint.Chromium.ChromeDriver
{
	namespace DevTools.Events
	{
		namespace Network
		{
			public class Base
			{
				public readonly static string Prefix1 = "DEVTOOLS EVENT Network.";
				public readonly static string Prefix2 = "DevTools WebSocket Event: Network.";

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
					public int status;

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
				public readonly static new string Prefix = Base.Prefix1 + "webSocket";
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

			public class LoadingFailed: Base
			{
				public readonly static string EventType = "loadingFailed";

				public string errorText;
				public bool? canceled;
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
			readonly static JsonSerializerSettings payloadParserSettings = new JsonSerializerSettings() { CheckAdditionalContent = false };
			readonly static Regex regex1 = new Regex(@"^DEVTOOLS EVENT (?<ns>\w+)\.(?<evt>\w+) ",
				RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
			readonly static Regex regex2 = new Regex(@"^DevTools WebSocket Event\: (?<ns>\w+)\.(?<evt>\w+) \w+ ",
				RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

			public StringSlice EventNamespace { get; private set; }
			public StringSlice EventType { get; private set; }
			public StringSlice Payload { get; private set; }

			public T ParsePayload<T>()
			{
				try
				{
					using (StringReader sr = new StringReader(Payload.Buffer))
					{
						Skip(sr, Payload.StartIndex);
						using (JsonReader reader = new JsonTextReader(sr))
						{
							return JsonSerializer.CreateDefault(payloadParserSettings).Deserialize<T>(reader);
						}
					}
				}
				catch (Exception)
				{
					return default(T);
				}
			}

			public static LogMessage Parse(string str)
			{
				var m = regex1.Match(str);
				if (!m.Success)
					m = regex2.Match(str);
				if (!m.Success)
					return null;
				return new LogMessage()
				{
					EventNamespace = new StringSlice(str, m.Groups[1]),
					EventType = new StringSlice(str, m.Groups[2]),
					Payload = new StringSlice(str, m.Length),
				};
			}

			static readonly char[] skipBuffer = new char[1024];

			private static void Skip(TextReader tr, int count)
			{
				for (int pos = 0; pos < count; pos += skipBuffer.Length)
				{
					tr.Read(skipBuffer, 0, Math.Min(skipBuffer.Length, count - pos));
				}
			}
		}
	};
}
