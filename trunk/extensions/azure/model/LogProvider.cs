using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace LogJoint.Azure
{
	public class LogProvider : LiveLogProvider
	{
		public interface IStrategy
		{
			IAzureDiagnosticLogsTable CreateTable(StorageAccount account);
			string GetEntryMessage(AzureDiagnosticLogEntry entry);
		};


		public LogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams, IStrategy strategy)
			:
			base(host, factory, connectParams)
		{
			try
			{
				this.strategy = strategy;
				this.azureConnectParams = new AzureConnectionParams(connectionParams);
				this.table = strategy.CreateTable(this.azureConnectParams.Account);

				StartLiveLogThread("WAD listening thread");
			}
			catch (Exception e)
			{
				trace.Error(e, "Failed to initialize WAD reader. Disposing what has been created so far.");
				Dispose();
				throw;
			}
		}

		protected override async Task LiveLogListen(CancellationToken stopEvt, LiveLogXMLWriter output)
		{
			using (trace.NewFrame)
			{
				try
				{
					if (azureConnectParams.Mode == AzureConnectionParams.LoadMode.FixedRange)
					{
						ReportBackgroundActivityStatus(true);
						foreach (var entry in AzureDiagnosticsUtils.LoadEntriesRange(
							table, new EntryPartition(azureConnectParams.From.Ticks), new EntryPartition(azureConnectParams.Till.Ticks), null, stopEvt))
						{
							WriteEntry(entry.Entry, output);
							if (stopEvt.IsCancellationRequested)
								return;
						}
						ReportBackgroundActivityStatus(false);
						return;
					}
					else if (azureConnectParams.Mode == AzureConnectionParams.LoadMode.Recent)
					{
						ReportBackgroundActivityStatus(true);
						var lastPartition = AzureDiagnosticsUtils.FindLastMessagePartitionKey(table, DateTime.UtcNow, stopEvt);
						if (lastPartition.HasValue)
						{
							var firstPartition = new EntryPartition(lastPartition.Value.Ticks + azureConnectParams.Period.Ticks);
							foreach (var entry in AzureDiagnosticsUtils.LoadEntriesRange(table, firstPartition, EntryPartition.MaxValue, null, stopEvt))
							{
								WriteEntry(entry.Entry, output);
								stopEvt.ThrowIfCancellationRequested();
							}
						}
						ReportBackgroundActivityStatus(false);
						return;
					}
				}
				catch (OperationCanceledException e)
				{
					trace.Error(e, "WAD live log thread cancelled");
				}
				catch (Exception e)
				{
					trace.Error(e, "WAD live log thread failed");
				}
			}
		}

		void WriteEntry(AzureDiagnosticLogEntry entry, LiveLogXMLWriter output)
		{
			XmlWriter writer = output.BeginWriteMessage(false);
			writer.WriteStartElement("m");
			writer.WriteAttributeString("d", Listener.FormatDate(new DateTime(entry.EventTickCount, DateTimeKind.Utc).ToLocalTime()));
			writer.WriteAttributeString("t", string.Format("{0}-{1}", entry.Pid, entry.Tid));
			if (entry.Level <= 2)
				writer.WriteAttributeString("s", "e");
			else if (entry.Level == 3)
				writer.WriteAttributeString("s", "w");
			writer.WriteString(strategy.GetEntryMessage(entry));
			writer.WriteEndElement();
			output.EndWriteMessage();
		}

		readonly IStrategy strategy;
		readonly IAzureDiagnosticLogsTable table;
		readonly AzureConnectionParams azureConnectParams;
	}

	public class StorageAccount
	{
		public StorageAccount(IConnectionParams connectParams)
		{
			var name = connectParams["name"];
			if (name == DevelopmentAccountName)
			{
				type = Type.DevelopmentAccount;
				this.name = DevelopmentAccountName;
			}
			else
			{
				type = Type.CloudAccount;
				this.name = name;
				this.key = connectParams["key"];
				bool.TryParse(connectParams["useHttps"] ?? null, out this.useHttps);
			}
		}
		public StorageAccount()
		{
			this.type = Type.DevelopmentAccount;
			this.name = DevelopmentAccountName;
		}
		public StorageAccount(string name, string key, bool useHttps)
		{
			this.type = Type.CloudAccount;
			this.name = name;
			this.key = key;
			this.useHttps = useHttps;
		}

		public override string ToString()
		{
			return ToConnectionString();
		}

		public string ToUserFriendlyString()
		{
			string userFriendlyName;
			if (type == Type.DevelopmentAccount)
				userFriendlyName = "development account";
			else
				userFriendlyName = name;
			return userFriendlyName;
		}

		public string ToConnectionString()
		{
			switch (type)
			{
				case Type.DevelopmentAccount:
					return DevelopmentAccountName;
				case Type.CloudAccount:
					return string.Format(
						"DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}",
						useHttps ? "https" : "http", name, key);
				default:
					return "";
			}
		}

		public CloudStorageAccount ToCloudStorageAccount()
		{
			return CloudStorageAccount.Parse(ToConnectionString());
		}

		public static readonly string DevelopmentAccountName = "UseDevelopmentStorage=true";

		public enum Type
		{
			DevelopmentAccount,
			CloudAccount
		};

		public Type AccountType { get { return type; } }
		public string AccountName { get { return name; } }
		public string AccountKey { get { return key; } }
		public bool UseHTPPS { get { return useHttps; } }

		public void SaveToConnectionParams(IConnectionParams connectParams)
		{
			switch (type)
			{
				case Type.DevelopmentAccount:
					connectParams["name"] = DevelopmentAccountName;
					break;
				case Type.CloudAccount:
					connectParams["name"] = name;
					connectParams["key"] = key;
					connectParams["useHttps"] = useHttps.ToString();
					break;
			}
		}

		Type type;
		string name;
		string key;
		bool useHttps;
	};

	internal static class ConnectionParamsConsts
	{
		internal static readonly string FromConnectionParam = "from";
		internal static readonly string TillConnectionParam = "till";
		internal static readonly string RecentConnectionParam = "recent";
		internal static readonly string LiveConnectionParam = "live";
		internal static readonly string DateConnectionParamFormat = "u";
		internal static readonly string TimespanConnectionParamFormat = "c";
	};

	public class AzureConnectionParams
	{
		public enum LoadMode
		{
			FixedRange,
			Recent
		};
		public StorageAccount Account { get; private set; }
		public LoadMode Mode { get; private set; }
		public DateTime From { get; private set; }
		public DateTime Till { get; private set; }
		public TimeSpan Period { get; private set; }
		public bool Live { get; private set; }

		public AzureConnectionParams(StorageAccount account, DateTime from, DateTime till)
		{
			Mode = LoadMode.FixedRange;
			Account = account;
			From = from;
			Till = till;
		}

		public AzureConnectionParams(StorageAccount account, TimeSpan period, bool liveFlag)
		{
			Mode = LoadMode.Recent;
			Account = account;
			Period = period;
			Live = liveFlag;
		}

		public AzureConnectionParams(IConnectionParams connectParams)
		{
			Account = new StorageAccount(connectParams);

			var fmtProv = CultureInfo.InvariantCulture.DateTimeFormat;
			Func<string, DateTime> parseDate = str => DateTime.ParseExact(str, ConnectionParamsConsts.DateConnectionParamFormat, fmtProv);
			Func<string, TimeSpan> parseTimeSpan = str => TimeSpan.ParseExact(str, ConnectionParamsConsts.TimespanConnectionParamFormat, fmtProv);

			var fromStr = connectParams[ConnectionParamsConsts.FromConnectionParam];
			var tillStr = connectParams[ConnectionParamsConsts.TillConnectionParam];
			var periodStr = connectParams[ConnectionParamsConsts.RecentConnectionParam];
			var liveStr = connectParams[ConnectionParamsConsts.LiveConnectionParam];
			if (fromStr != null)
			{
				Mode = LoadMode.FixedRange;
				From = parseDate(fromStr);
				Till = parseDate(tillStr);
			}
			else if (periodStr != null)
			{
				Mode = LoadMode.Recent;
				Period = parseTimeSpan(periodStr);
				bool live;
				bool.TryParse(connectParams[ConnectionParamsConsts.LiveConnectionParam] ?? "", out live);
				Live = live;
			}
			else
			{
				throw new InvalidConnectionParamsException("Bad connection params for Azure log provider");
			}
		}

		public IConnectionParams ToConnectionParams()
		{
			var ret = new ConnectionParams();
			if (Mode == LoadMode.FixedRange)
			{
				Account.SaveToConnectionParams(ret);
				var fromStr = ret[ConnectionParamsConsts.FromConnectionParam] = From.ToString(ConnectionParamsConsts.DateConnectionParamFormat);
				var tillStr = ret[ConnectionParamsConsts.TillConnectionParam] = Till.ToString(ConnectionParamsConsts.DateConnectionParamFormat);
				ret[ConnectionParamsKeys.IdentityConnectionParam] = string.Format(
					"wad-{0}-from:{1}-fill:{2}", Account.AccountName, fromStr, tillStr);
			}
			else if (Mode == LoadMode.Recent)
			{
				Account.SaveToConnectionParams(ret);
				var timeSpanStr = ret[ConnectionParamsConsts.RecentConnectionParam] =
					Period.ToString(ConnectionParamsConsts.TimespanConnectionParamFormat);
				var liveStr = ret[ConnectionParamsConsts.LiveConnectionParam] = Live.ToString();
				ret[ConnectionParamsKeys.IdentityConnectionParam] = string.Format(
					"wad-{0}-recent:{1}-live:{2}", Account.AccountName, timeSpanStr, liveStr);
			}
			return ret;
		}

		public string ToUserFriendlyString(string prefix)
		{
			var builder = new StringBuilder();
			builder.AppendFormat("{0} - {1}. ", prefix, Account.ToUserFriendlyString());
			if (Mode == LoadMode.FixedRange)
				builder.AppendFormat("From {0} Till {1}", From, Till);
			else if (Mode == LoadMode.Recent)
				builder.AppendFormat("Last {0}{1}", -Period, Live ? ", auto-refreshing" : "");
			return builder.ToString();
		}
	};

	public class WADLogsTableProviderStrategy : LogProvider.IStrategy
	{
		public IAzureDiagnosticLogsTable CreateTable(StorageAccount account)
		{
			return AzureDiagnosticLogsTable<WADLogsTableEntry>.CreateTable(account, "WADLogsTable");
		}

		public string GetEntryMessage(AzureDiagnosticLogEntry entry)
		{
			var wadLogsTableEntry = (WADLogsTableEntry)entry;
			return string.Format("{0}\n  RoleInstance={1}\n  DeploymentId={2}", wadLogsTableEntry.Message, entry.RoleInstance ?? "", entry.DeploymentId ?? "");
		}
	};

	public class WADWindowsEventLogsTableProviderStrategy : LogProvider.IStrategy
	{
		public IAzureDiagnosticLogsTable CreateTable(StorageAccount account)
		{
			return AzureDiagnosticLogsTable<WADWindowsEventLogsTableEntry>.CreateTable(account, "WADWindowsEventLogsTable");
		}

		public string GetEntryMessage(AzureDiagnosticLogEntry entry)
		{
			var eventLogEntry = (WADWindowsEventLogsTableEntry)entry;
			return string.Format("{0}\n  RoleInstance={1}\n  DeploymentId={2}\n  Channel={3}", eventLogEntry.Description, 
				entry.RoleInstance ?? "", entry.DeploymentId ?? "", eventLogEntry.Channel ?? "");
		}
	};

	public class WADDiagnosticInfrastructureLogsTableProviderStrategy : LogProvider.IStrategy
	{
		public IAzureDiagnosticLogsTable CreateTable(StorageAccount account)
		{
			return AzureDiagnosticLogsTable<WADDiagnosticInfrastructureLogsTableEntry>.CreateTable(account, "WADDiagnosticInfrastructureLogsTable");
		}

		public string GetEntryMessage(AzureDiagnosticLogEntry entry)
		{
			var infraLogEntry = (WADDiagnosticInfrastructureLogsTableEntry)entry;
			var builder = new StringBuilder();
			builder.AppendFormat("{0}\n  RoleInstance={1}\n  DeploymentId={2}", infraLogEntry.Message,
				entry.RoleInstance ?? "", entry.DeploymentId ?? "");
			if (!string.IsNullOrEmpty(infraLogEntry.Function) || infraLogEntry.Line != 0 || infraLogEntry.MDRESULT != 0)
			{
				builder.AppendFormat("\n  Function={0}:{1}", infraLogEntry.Function ?? "<unknown>", infraLogEntry.Line.ToString());
				if (infraLogEntry.MDRESULT != 0)
					builder.AppendFormat(" -> {0}", infraLogEntry.MDRESULT);
			}
			if (!string.IsNullOrEmpty(infraLogEntry.ErrorCodeMsg) || infraLogEntry.ErrorCode != 0)
			{
				builder.AppendFormat("\n  Error={0}", infraLogEntry.ErrorCode);
				if (!string.IsNullOrEmpty(infraLogEntry.ErrorCodeMsg))
				{
					builder.AppendFormat("({0})", infraLogEntry.ErrorCodeMsg);
				}
			}
			return builder.ToString();
		}
	};

	public class Factory : ILogProviderFactory
	{
		public static readonly string uiTypeKey = "azure";

		public Factory(string formatName, string formatDescription, LogProvider.IStrategy providerStrategy, ITempFilesManager tempFiles)
		{
			this.formatName = formatName;
			this.formatDescription = formatDescription;
			this.providerStrategy = providerStrategy;
			this.tempFiles = tempFiles;
		}

		public static void RegisterFactories(ILogProviderFactoryRegistry registry, ITempFilesManager tempFiles)
		{
			registry.Register(new Factory(
				"Azure Diagnostics Log",
				"Windows Azure Diagnostics log that is stored in Azure Tables Storage table (WADLogsTable)",
				new WADLogsTableProviderStrategy(), 
				tempFiles
			));
			registry.Register(new Factory(
				"Azure Diagnostics Windows Event Log",
				"Windows Azure operating system event log collected and stored in Azure Tables Storage table (WADWindowsEventLogsTable)",
				new WADWindowsEventLogsTableProviderStrategy(),
				tempFiles
			));
			registry.Register(new Factory(
				"Azure Diagnostics Infrastructure Log",
				"Windows Azure Diagnostics infrastructure log collected and stored in Azure Tables Storage table (WADDiagnosticInfrastructureLogsTable)",
				new WADDiagnosticInfrastructureLogsTableProviderStrategy(),
				tempFiles
			));
		}

		public IConnectionParams CreateParams(StorageAccount account, DateTime from, DateTime till)
		{
			return new AzureConnectionParams(account, from, till).ToConnectionParams();
		}

		public IConnectionParams CreateParams(StorageAccount account, TimeSpan negativeTimeSpanSinceNow, bool liveLogFlag)
		{
			return new AzureConnectionParams(account, negativeTimeSpanSinceNow, liveLogFlag).ToConnectionParams();
		}

		public void TestAccount(StorageAccount account)
		{
			try
			{
				var table = AzureDiagnosticLogsTable<AzureDiagnosticLogEntry>.CreateTable(account, "WADLogsTable");
				AzureDiagnosticsUtils.FindFirstMessagePartitionKey(table);
			}
			catch (Exception exception)
			{
				RethrowStorageExceptionWithUserFriendlyMessage(exception);
				throw;
			}
		}

		private static void RethrowStorageExceptionWithUserFriendlyMessage(Exception exception)
		{
			for (var e = exception; e != null; e = e.InnerException)
			{
				var dataException = e as System.Data.Services.Client.DataServiceClientException;
				if (dataException != null)
				{
					var msg = dataException.Message;
					try
					{
						string dataservicesNs = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
						var responseBody = XDocument.Parse(msg);
						var errNode = responseBody.Element(XName.Get("error", dataservicesNs));
						if (errNode != null)
						{
							var msgNode = errNode.Element(XName.Get("message", dataservicesNs));
							if (msgNode != null)
							{
								msg = string.Format("{0}\n\nServer response:\n{1}", msgNode.Value, msg);
							}
						}
					}
					catch
					{
						// response digging failed. ignore.
					}
					throw new Exception(msg, exception);
				}
			}
		}

		string ILogProviderFactory.CompanyName
		{
			get { return "Microsoft"; }
		}

		string ILogProviderFactory.FormatName
		{
			get { return formatName; }
		}

		string ILogProviderFactory.FormatDescription
		{
			get { return formatDescription; }
		}

		string ILogProviderFactory.UITypeKey { get { return uiTypeKey; } }

		string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return new AzureConnectionParams(connectParams).ToUserFriendlyString(formatName);
		}

		string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), this.tempFiles);
		}

		ILogProvider ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, this, connectParams, providerStrategy);
		}

		IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

		LogProviderFactoryFlag ILogProviderFactory.Flags
		{
			get { return LogProviderFactoryFlag.None; }
		}

		readonly string formatName;
		readonly string formatDescription;
		readonly LogProvider.IStrategy providerStrategy;
		readonly ITempFilesManager tempFiles;
	};
}
