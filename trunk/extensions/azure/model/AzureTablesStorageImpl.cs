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
	public class AzureDiagnosticLogsTable : IAzureDiagnosticLogsTable
	{
		private AzureDiagnosticLogsTable(CloudStorageAccount account)
		{
			this.account = account;
			this.client = new CloudTableClient(account.TableEndpoint.AbsoluteUri, account.Credentials);
		}

		public static AzureDiagnosticLogsTable CreateDevelopmentTable()
		{
			return new AzureDiagnosticLogsTable(CloudStorageAccount.Parse("UseDevelopmentStorage=true"));
		}

		public static AzureDiagnosticLogsTable CreateCloudTable(CloudStorageAccount cloudAccount)
		{
			return new AzureDiagnosticLogsTable(cloudAccount);
		}

		public static AzureDiagnosticLogsTable CreateTable(StorageAccount account)
		{
			return account.AccountType == StorageAccount.Type.DevelopmentAccount ?
				AzureDiagnosticLogsTable.CreateDevelopmentTable() : new AzureDiagnosticLogsTable(account.ToCloudStorageAccount());
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
				var segment = Task.Factory.FromAsync<ResultSegment<AzureDiagnosticLogEntry>>(q.BeginExecuteSegmented(continuation, null, null), q.EndExecuteSegmented).Result;
				foreach (var i in segment.Results)
					yield return i;
				if (!segment.HasMoreResults)
					break;
				continuation = segment.ContinuationToken;
			}
		}

		DataServiceQuery<AzureDiagnosticLogEntry> CreateQuery()
		{
			return client.GetDataServiceContext().CreateQuery<AzureDiagnosticLogEntry>(logsTableName);
		}

		readonly CloudStorageAccount account;
		readonly CloudTableClient client;
		readonly static string logsTableName = "WADLogsTable";
	}
}
