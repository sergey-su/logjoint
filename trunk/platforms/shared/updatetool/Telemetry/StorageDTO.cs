using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace LogJoint.UpdateTool.Telemetry
{
	public class AzureStorageEntry : TableEntity
	{
		public string installationId { get; set; }
		public string timezone { get; set; }
		public string buildTime { get; set; }
		public string sourceRevision { get; set; }
		public string id { get; set; }
		public string started { get; set; }
		public long duration { get; set; }
		public string totalNfOfLogs { get; set; }
		public int maxNfOfSimultaneousLogs { get; set; }
		public bool finalized { get; set; }
		public string exceptions { get; set; }
		public string usedFeatures { get; set; }

		public DateTime SessionStartDate
		{
			get
			{
				return DateTime.ParseExact(started, "o", null);
			}
		}

		public int TotalNrOfLogs
		{
			get
			{
				return totalNfOfLogs != null ? int.Parse(totalNfOfLogs) : 0;
			}
		}
	};
}
