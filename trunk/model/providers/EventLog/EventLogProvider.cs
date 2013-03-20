using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using System.ComponentModel;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace LogJoint.WindowsEventLog
{
	public class LogProvider : LiveLogProvider
	{
		public LogProvider(ILogProviderHost host, IConnectionParams connectParams)
			:
			base(host, 
				WindowsEventLog.Factory.Instance, 
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

		public override void Dispose()
		{
			using (trace.NewFrame)
			{
				trace.Info("Calling base destructor");
				base.Dispose();
			}
		}

		protected override void LiveLogListen(ManualResetEvent stopEvt, LiveLogXMLWriter output)
		{
			using (host.Trace.NewFrame)
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
									if (stopEvt.WaitOne(0))
										return;
									WriteEvent(eventInstance, output);
									lastReadBookmark = eventInstance.Bookmark;
								}
							}
						}
						ReportBackgroundActivityStatus(false);
						if (eventLogIdentity.Type == EventLogIdentity.EventLogType.File)
							break;
						if (stopEvt.WaitOne(TimeSpan.FromSeconds(10)))
							return;
					}
				}
				catch (Exception e)
				{
					host.Trace.Error(e, "EVT live log thread failed");
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
			return ParseIdentityString(connectParams[ConnectionParamsUtils.IdentityConnectionParam]);
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
		public static readonly Factory Instance = new Factory();

		static Factory()
		{
			LogProviderFactoryRegistry.DefaultInstance.Register(Instance);
		}

		public IConnectionParams CreateParamsFromIdentity(EventLogIdentity identity)
		{
			ConnectionParams p = new ConnectionParams();
			p[ConnectionParamsUtils.IdentityConnectionParam] = identity.ToIdentityString();
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

		#region ILogProviderFactory Members

		public string CompanyName
		{
			get { return "Microsoft"; }
		}

		public string FormatName
		{
			get { return "Windows Event Log"; }
		}

		public string FormatDescription
		{
			get { return "Windows Event Log files or live logs"; }
		}

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return factory.CreateWindowsEventLogUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return "Windows Event Log: " + EventLogIdentity.FromConnectionParams(connectParams).ToUserFriendlyString();
		}

		public string GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			var cp = originalConnectionParams.Clone(true);
			cp[ConnectionParamsUtils.PathConnectionParam] = null;
			return cp;
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, connectParams);
		}

		public IFormatViewOptions ViewOptions { get { return FormatViewOptions.NoRawView; } }

		public LogFactoryFlag Flags
		{
			get { return LogFactoryFlag.SupportsDejitter | LogFactoryFlag.DejitterEnabled; }
		}

		#endregion
	};
}
