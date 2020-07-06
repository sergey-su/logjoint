using System;
using System.Threading;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;

namespace LogJoint.WindowsEventLog
{
	public class LogProvider : LiveLogProvider
	{
		public LogProvider(ILogProviderHost host, IConnectionParams connectParams, Factory factory)
			:
			base(host,
				factory,
				connectParams,
				new DejitteringParams() { JitterBufferSize = 25 }
			)
		{
			using (trace.NewFrame)
			{
				try
				{
					eventLogIdentity = EventLogIdentity.FromConnectionParams(connectParams);
					StartLiveLogThread("EventLog listening thread");
				}
				catch (Exception e)
				{
					trace.Error(e, "Failed to initialize Windows Event Log reader. Disposing what has been created so far.");
					Dispose();
					throw;
				}
			}
		}

		public override async Task Dispose()
		{
			using (trace.NewFrame)
			{
				trace.Info("Calling base destructor");
				await base.Dispose();
			}
		}

		public override string GetTaskbarLogName()
		{
			if (eventLogIdentity.FileName != null)
				return ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(eventLogIdentity.FileName);
			return eventLogIdentity.LogName;
		}

		protected override async Task LiveLogListen(CancellationToken stopEvt, LiveLogXMLWriter output)
		{
			using (this.trace.NewFrame)
			{
				try
				{
					var query = CreateQuery();
					for (EventBookmark lastReadBookmark = null; ; )
					{
						ReportBackgroundActivityStatus(true);
						using (var reader = new EventLogReader(query, lastReadBookmark))
						{
							for (; ; )
							{
								using (var eventInstance = reader.ReadEvent())
								{
									if (eventInstance == null)
										break;
									if (stopEvt.IsCancellationRequested)
										return;
									WriteEvent(eventInstance, output);
									lastReadBookmark = eventInstance.Bookmark;
								}
							}
						}
						ReportBackgroundActivityStatus(false);
						if (eventLogIdentity.Type == EventLogIdentity.EventLogType.File)
							break;
						if (stopEvt.IsCancellationRequested)
							return;
					}
				}
				catch (Exception e)
				{
					this.trace.Error(e, "EVT live log thread failed");
				}
			}
		}

		EventLogQuery CreateQuery()
		{
			switch (eventLogIdentity.Type)
			{
				case EventLogIdentity.EventLogType.File:
					return new EventLogQuery(eventLogIdentity.FileName, PathType.FilePath);
				case EventLogIdentity.EventLogType.LocalLiveLog:
					return new EventLogQuery(eventLogIdentity.LogName, PathType.LogName);
				case EventLogIdentity.EventLogType.RemoteLiveLog:
					var session = new EventLogSession(eventLogIdentity.MachineName);
					return new EventLogQuery(eventLogIdentity.LogName, PathType.LogName) { Session = session };
				default:
					throw new InvalidOperationException();
			}
		}

		static string GetEventThreadId(EventRecord eventRecord)
		{
			var threadIdStr = eventRecord.ThreadId.HasValue ? eventRecord.ThreadId.Value.ToString() : "N/A";
			if (eventRecord.ProcessId.HasValue)
				return string.Format("{0}-{1}", eventRecord.ProcessId.Value, threadIdStr);
			else
				return threadIdStr;
		}

		static string GetEventDescription(EventRecord eventRecord)
		{
			string descr;
			try
			{
				descr = (eventRecord.FormatDescription() ?? "").Trim();
			}
			catch (EventLogException)
			{
				descr = "";
			}
			string keywords;
			try
			{
				keywords = string.Join(", ", eventRecord.KeywordsDisplayNames);
			}
			catch
			{
				keywords = "";
			}
			return string.Format("{0}{1}Event {2} from {3}{4}{5}", 
				descr, descr.Length > 0 ? ". " : "", 
				eventRecord.Id, 
				eventRecord.ProviderName, 
				keywords.Length > 0 ? ", keywords=" : "", keywords);
		}

		static string GetEventSeverity(EventRecord eventRecord)
		{
			if (!eventRecord.Level.HasValue)
				return null;
			var level = (StandardEventLevel) eventRecord.Level.Value;
			switch (level)
			{
				case StandardEventLevel.Error:
				case StandardEventLevel.Critical:
					return "e";
				case StandardEventLevel.Warning:
					return "w";
				default:
					return null;
			}
		}

		void WriteEvent(EventRecord eventRecord, LiveLogXMLWriter output)
		{
			XmlWriter writer = output.BeginWriteMessage(false);
			writer.WriteStartElement("m");
			writer.WriteAttributeString("d", Listener.FormatDate(eventRecord.TimeCreated.GetValueOrDefault()));
			writer.WriteAttributeString("t", GetEventThreadId(eventRecord));
			var s = GetEventSeverity(eventRecord);
			if (s != null)
				writer.WriteAttributeString("s", s);
			writer.WriteString(GetEventDescription(eventRecord));
			writer.WriteEndElement();
			output.EndWriteMessage();
		}

		readonly EventLogIdentity eventLogIdentity;
	}

	public class EventLogIdentity
	{
		public enum EventLogType
		{
			File,
			LocalLiveLog,
			RemoteLiveLog
		};

		public static EventLogIdentity FromConnectionParams(IConnectionParams connectParams)
		{
			return ParseIdentityString(connectParams[ConnectionParamsKeys.IdentityConnectionParam]);
		}

		public static EventLogIdentity FromLiveLogParams(string machineName, string logName)
		{
			return new EventLogIdentity()
			{
				machineName = string.IsNullOrWhiteSpace(machineName) ? "." : machineName.Trim(),
				logName = logName
			};
		}

		public static EventLogIdentity FromFileName(string fileName)
		{
			return new EventLogIdentity()
			{
				fileName = fileName
			};
		}

		public static EventLogIdentity ParseIdentityString(string identityString)
		{
			var m = identityRegex.Match(identityString);
			if (!m.Success)
				throw new ArgumentException("Cannot parse windows event log identity " + identityString);
			if (m.Groups["fname"].Success)
				return FromFileName(m.Groups["fname"].Value);
			else if (m.Groups["remoteLog"].Success)
				return FromLiveLogParams(m.Groups["machine"].Value, m.Groups["remoteLog"].Value);
			else
				return FromLiveLogParams(".", m.Groups["localLog"].Value);
		}

		public EventLogType Type
		{
			get
			{
				if (fileName != null)
					return EventLogType.File;
				if (machineName != ".")
					return EventLogType.RemoteLiveLog;
				return EventLogType.LocalLiveLog;
			}
		}

		public string FileName { get { return fileName; } }
		public string MachineName { get { return machineName; } }
		public string LogName { get { return logName; } }

		public string ToIdentityString()
		{
			if (fileName != null)
				return "f:" + fileName;
			else if (machineName == ".")
				return "l:" + logName;
			else
				return string.Format("r:{0}/{1}", machineName, logName);
		}

		public string ToUserFriendlyString()
		{
			if (fileName != null)
				return fileName;
			else
				return string.Format("{0}/{1}", machineName, logName);
		}

		static Regex identityRegex = new Regex(@"^(f\:(?<fname>.+))|(r\:(?<machine>[^\/]+)\/(?<remoteLog>.+))|(l\:(?<localLog>.+))$",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		string machineName;
		string logName;
		string fileName;
	};

	public class Factory : ILogProviderFactory
	{
		public IConnectionParams CreateParamsFromIdentity(EventLogIdentity identity)
		{
			ConnectionParams p = new ConnectionParams();
			p[ConnectionParamsKeys.IdentityConnectionParam] = identity.ToIdentityString();
			return p;
		}

		public IConnectionParams CreateParamsFromFileName(string fileName)
		{
			return CreateParamsFromIdentity(EventLogIdentity.FromFileName(fileName));
		}

		public IConnectionParams CreateParamsFromEventLogName(string machineName, string eventLogName)
		{
			return CreateParamsFromIdentity(EventLogIdentity.FromLiveLogParams(machineName, eventLogName));
		}

		string ILogProviderFactory.CompanyName
		{
			get { return "Microsoft"; }
		}

		string ILogProviderFactory.FormatName
		{
			get { return "Windows Event Log"; }
		}

		string ILogProviderFactory.FormatDescription
		{
			get { return "Windows Event Log files or live logs"; }
		}

		string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.WindowsEventLogProviderUIKey; } }

		string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return "Windows Event Log: " + EventLogIdentity.FromConnectionParams(connectParams).ToUserFriendlyString();
		}

		string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			var cp = originalConnectionParams.Clone(true);
			cp[ConnectionParamsKeys.PathConnectionParam] = null;
			ConnectionParamsUtils.RemoveInitialTimeOffset(cp);
			return cp;
		}

		ILogProvider ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, connectParams, this);
		}

		IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

		LogProviderFactoryFlag ILogProviderFactory.Flags
		{
			get { return LogProviderFactoryFlag.SupportsDejitter | LogProviderFactoryFlag.DejitterEnabled; }
		}
	};
}
