using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using System.Threading.Tasks;

namespace LogJoint.Azure
{
	public class AzureDiagnosticLogsTable<EntryType> : IAzureDiagnosticLogsTable where EntryType: AzureDiagnosticLogEntry
	{
		private AzureDiagnosticLogsTable(CloudStorageAccount account, string logsTableName)
		{
			this.account = account;
			this.logsTableName = logsTableName;
			this.client = new CloudTableClient(account.TableEndpoint.AbsoluteUri, account.Credentials);
		}

		public static AzureDiagnosticLogsTable<EntryType> CreateDevelopmentTable(string logsTableName)
		{
			return new AzureDiagnosticLogsTable<EntryType>(CloudStorageAccount.Parse("UseDevelopmentStorage=true"), logsTableName);
		}

		public static AzureDiagnosticLogsTable<EntryType> CreateCloudTable(CloudStorageAccount cloudAccount, string logsTableName)
		{
			return new AzureDiagnosticLogsTable<EntryType>(cloudAccount, logsTableName);
		}

		public static AzureDiagnosticLogsTable<EntryType> CreateTable(StorageAccount account, string logsTableName)
		{
			return account.AccountType == StorageAccount.Type.DevelopmentAccount ?
				AzureDiagnosticLogsTable<EntryType>.CreateDevelopmentTable(logsTableName) : new AzureDiagnosticLogsTable<EntryType>(account.ToCloudStorageAccount(), logsTableName);
		}

		public AzureDiagnosticLogEntry GetFirstEntry()
		{
			return CreateQuery().Take(1).AsTableServiceQuery().FirstOrDefault();
		}

		public AzureDiagnosticLogEntry GetFirstEntryOlderThan(string partitionKey)
		{
			return (from e in CreateQuery()
					 where e.PartitionKey.CompareTo(partitionKey) > 0
					 select e).Take(1).AsTableServiceQuery().FirstOrDefault();
		}

		public IEnumerable<AzureDiagnosticLogEntry> GetEntriesInRange(string beginPartitionKey, string endPartitionKey, int? entriesLimit)
		{
			var q = (from e in CreateQuery()
					where e.PartitionKey.CompareTo(beginPartitionKey) >= 0 && e.PartitionKey.CompareTo(endPartitionKey) < 0
					select e).AsTableServiceQuery();
			if (entriesLimit.HasValue)
				q = q.Take(entriesLimit.Value).AsTableServiceQuery();
			for (ResultContinuation continuation = null; ; )
			{
				var segment = Task.Factory.FromAsync<ResultSegment<EntryType>>(q.BeginExecuteSegmented(continuation, null, null), q.EndExecuteSegmented).Result;
				foreach (var i in segment.Results)
					yield return i;
				if (!segment.HasMoreResults)
					break;
				continuation = segment.ContinuationToken;
			}
		}

		DataServiceQuery<EntryType> CreateQuery()
		{
			return client.GetDataServiceContext().CreateQuery<EntryType>(logsTableName);
		}

		readonly CloudStorageAccount account;
		readonly CloudTableClient client;
		readonly string logsTableName;
	}
}
