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

namespace LogJoint.Azure
{
	public class LogProvider : LiveLogProvider
	{
		public LogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams)
			:
			base(host, factory, connectParams)
		{
			try
			{
				this.azureConnectParams = new AzureConnectionParams(connectionParams);
				this.table = AzureDiagnosticLogsTable.CreateTable(this.azureConnectParams.Account);

				StartLiveLogThread("WAD listening thread");
			}
			catch (Exception e)
			{
				trace.Error(e, "Failed to initialize WAD reader. Disposing what has been created so far.");
				Dispose();
				throw;
			}
		}

		protected override void LiveLogListen(ManualResetEvent stopEvt, LiveLogXMLWriter output)
		{
			using (host.Trace.NewFrame)
			{
				try
				{
					if (azureConnectParams.Mode == AzureConnectionParams.LoadMode.FixedRange)
					{
						foreach (var entry in AzureDiagnosticsUtils.LoadEntriesRange(
							table, new EntryPartition(azureConnectParams.From.Ticks), new EntryPartition(azureConnectParams.Till.Ticks), null))
						{
							WriteEntry(entry.Entry, output);
							if (stopEvt.WaitOne(0))
								return;
						}
						return;
					}
					else if (azureConnectParams.Mode == AzureConnectionParams.LoadMode.Recent)
					{
						var lastPartition = AzureDiagnosticsUtils.FindLastMessagePartitionKey(table, DateTime.UtcNow);
						if (lastPartition.HasValue)
						{
							var firstPartition = new EntryPartition(lastPartition.Value.Ticks + azureConnectParams.Period.Ticks);
							foreach (var entry in AzureDiagnosticsUtils.LoadEntriesRange(table, firstPartition, lastPartition.Value, null))
							{
								WriteEntry(entry.Entry, output);
								if (stopEvt.WaitOne(0))
									return;
							}
						}
						return;
					}
				}
				catch (Exception e)
				{
					host.Trace.Error(e, "WAF live log thread failed");
				}
			}
		}

		void WriteEntry(AzureDiagnosticLogEntry entry, LiveLogXMLWriter output)
		{
			XmlWriter writer = output.BeginWriteMessage(false);
			writer.WriteStartElement("m");
			writer.WriteAttributeString("d", Listener.FormatDate(new DateTime(entry.EventTickCount, DateTimeKind.Utc).ToLocalTime()));
			writer.WriteAttributeString("t", string.Format("{0}-{1}", entry.Pid, entry.Tid));
			writer.WriteString(entry.Message);
			writer.WriteEndElement();
			output.EndWriteMessage();
		}

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
			return "Azure Diagnostics Log (" + userFriendlyName + ")";
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
				ret[ConnectionParamsUtils.IdentityConnectionParam] = string.Format(
					"wad-{0}-from:{1}-fill:{2}", Account.AccountName, fromStr, tillStr);
			}
			else if (Mode == LoadMode.Recent)
			{
				Account.SaveToConnectionParams(ret);
				var timeSpanStr = ret[ConnectionParamsConsts.RecentConnectionParam] =
					Period.ToString(ConnectionParamsConsts.TimespanConnectionParamFormat);
				var liveStr = ret[ConnectionParamsConsts.LiveConnectionParam] = Live.ToString();
				ret[ConnectionParamsUtils.IdentityConnectionParam] = string.Format(
					"wad-{0}-recent:{1}-live:{2}", Account.AccountName, timeSpanStr, liveStr);
			}
			return ret;
		}

		public string ToUserFriendlyString()
		{
			var builder = new StringBuilder(Account.ToUserFriendlyString());
			builder.Append(" ");
			if (Mode == LoadMode.FixedRange)
				builder.AppendFormat("From {0} Till {1}", From, Till);
			else if (Mode == LoadMode.Recent)
				builder.AppendFormat("Last {0}{1}", -Period, Live ? ", auto-refreshing" : "");
			return builder.ToString();
		}
	};

	public class Factory : ILogProviderFactory
	{
		public static readonly Factory Instance = new Factory();

		static Factory()
		{
			LogProviderFactoryRegistry.DefaultInstance.Register(Instance);
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
			var table = AzureDiagnosticLogsTable.CreateTable(account);
			AzureDiagnosticsUtils.FindFirstMessagePartitionKey(table);
		}

		#region ILogReaderFactory Members

		public string CompanyName
		{
			get { return "Microsoft"; }
		}

		public string FormatName
		{
			get { return "Azure Diagnostics Log"; }
		}

		public string FormatDescription
		{
			get { return "Windows Azure Diagnostics log that is stored in Azure Tables Storage table (WADLogsTable)"; }
		}

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return new FactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return new AzureConnectionParams(connectParams).ToUserFriendlyString();
		}

		public string GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemovePathParamIfItRefersToTemporaryFile(originalConnectionParams.Clone(true), TempFilesManager.GetInstance());
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, this, connectParams);
		}

		public IFormatViewOptions ViewOptions { get { return FormatViewOptions.NoRawView; } }

		public LogFactoryFlag Flags
		{
			get
			{
				return LogFactoryFlag.None;
			}
		}

		#endregion
	};
}
