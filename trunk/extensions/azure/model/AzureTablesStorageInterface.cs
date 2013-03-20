using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.Azure
{
	/// <summary>
	/// IAzureDiagnosticLogsTable interface abtracts access to an Azure logs table (such as WADLogsTable, WADWindowsEventLogsTable).
	/// Abtraction allows not to reference platform-specific Azure assemblies from 
	/// this model assembly and to help mock Azure stuff in unit test.
	/// Normally IAzureDiagnosticLogsTable is implemented using CloudTableClient class.
	/// </summary>
	public interface IAzureDiagnosticLogsTable
	{
		AzureDiagnosticLogEntry GetFirstEntry();
		AzureDiagnosticLogEntry GetFirstEntryOlderThan(string partitionKey);
		IEnumerable<AzureDiagnosticLogEntry> GetEntriesInRange(string beginPartitionKey, string endPartitionKey, int? entriesLimit);
	}

	public class AzureDiagnosticLogEntry
	{
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTime Timestamp { get; set; }

		public long EventTickCount { get; set; }
		public string DeploymentId { get; set; }
		public string Role { get; set; }
		public string RoleInstance { get; set; }
		public int Pid { get; set; }
		public int Tid { get; set; }
		public int Level { get; set; }
	};

	/// <summary>
	/// Entry of WADLogsTable table
	/// </summary>
	public class WADLogsTableEntry : AzureDiagnosticLogEntry
	{
		public string Message { get; set; }
		public int EventId { get; set; }
	}

	/// <summary>
	/// Entry of WADWindowsEventLogsTable table
	/// </summary>
	public class WADWindowsEventLogsTableEntry : AzureDiagnosticLogEntry
	{
		public string Description { get; set; }
		public string Channel { get; set; }
		public string ProviderName { get; set; }
	}

	/// <summary>
	/// Entry of WADDiagnosticInfrastructureLogsTable table
	/// </summary>
	public class WADDiagnosticInfrastructureLogsTableEntry : AzureDiagnosticLogEntry
	{
		public string Function { get; set; }
		public int Line { get; set; }
		public int MDRESULT { get; set; }
		public int ErrorCode { get; set; }
		public string ErrorCodeMsg { get; set; }
		public string Message { get; set; }
	};

	public struct IndexedAzureDiagnosticLogEntry
	{
		public AzureDiagnosticLogEntry Entry;
		public int IndexWithinPartition;
		public IndexedAzureDiagnosticLogEntry(AzureDiagnosticLogEntry entry, int indexWithinPartition)
		{
			Entry = entry;
			IndexWithinPartition = indexWithinPartition;
		}
	}
}
