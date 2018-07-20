using LogJoint.Analytics.Correlation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Chromium.Correlation
{
	public class NodeDetectionToken : ISameNodeDetectionToken
	{
		readonly HashSet<uint> processIds;
		readonly Dictionary<string, ICECandidateInfo> iceCandidates;
		readonly Dictionary<string, ConsoleLogEntry> logEntries;

		public NodeDetectionToken(
			IEnumerable<uint> processIds,
			IEnumerable<ICECandidateInfo> iceCandidates = null,
			IEnumerable<ConsoleLogEntry> uniqueLogEntries = null
		)
		{
			this.processIds = processIds.ToHashSet();
			this.iceCandidates = (iceCandidates ?? Enumerable.Empty<ICECandidateInfo>()).ToDictionary(c => c.Id);
			this.logEntries = (uniqueLogEntries ?? Enumerable.Empty<ConsoleLogEntry>()).ToDictionary(l => l.LogText);
		}

		public struct ICECandidateInfo
		{
			public readonly string Id;
			public readonly DateTime CreationTime;
			public ICECandidateInfo(string id, DateTime creationTime)
			{
				this.Id = id;
				this.CreationTime = creationTime;
			}
		};


		[DebuggerDisplay("{LogText}")]
		public struct ConsoleLogEntry
		{
			public readonly string LogText;
			public readonly DateTime Timestamp;
			public ConsoleLogEntry(string txt, DateTime ts)
			{
				this.LogText = txt;
				int maxLen = 1000;
				if (this.LogText.Length > maxLen)
					this.LogText = this.LogText.Substring(0, maxLen);
				this.Timestamp = ts;
			}
		};

		SameNodeDetectionResult ISameNodeDetectionToken.DetectSameNode(ISameNodeDetectionToken otherNodeToken)
		{
			var otherChromiumNode = otherNodeToken as NodeDetectionToken;
			if (otherChromiumNode == null)
				return null;
			if (!this.processIds.Overlaps(otherChromiumNode.processIds))
				return null;
			foreach (var candidate in this.iceCandidates)
			{
				ICECandidateInfo otherCandidate;
				if (otherChromiumNode.iceCandidates.TryGetValue(candidate.Key, out otherCandidate))
				{
					var diff = Math.Round((candidate.Value.CreationTime - otherCandidate.CreationTime).TotalMinutes);
					return new SameNodeDetectionResult()
					{
						TimeDiff = TimeSpan.FromMinutes(diff)
					};
				}
			}

			// console logging matching is used to match chromedebug and chromedriver logs
			// that both record console logging
			var loggingDiffs = new Dictionary<int, int>();
			foreach (var log in this.logEntries)
			{
				ConsoleLogEntry otherLog;
				if (otherChromiumNode.logEntries.TryGetValue(log.Key, out otherLog))
				{
					var diff = (int)Math.Round((log.Value.Timestamp - otherLog.Timestamp).TotalMinutes);
					int count = 0;
					loggingDiffs.TryGetValue(diff, out count);
					loggingDiffs[diff] = count + 1;
				}
			}
			var topLogDiffs = loggingDiffs.OrderByDescending(x => x.Value).Take(2).ToArray();
			var minLogCount = 5;
			if ((topLogDiffs.Length == 1 && topLogDiffs[0].Value > minLogCount)
			 || (topLogDiffs.Length == 2 && (topLogDiffs[0].Value - topLogDiffs[1].Value) > minLogCount))
			{
				return new SameNodeDetectionResult()
				{
					TimeDiff = TimeSpan.FromMinutes(topLogDiffs[0].Key)
				};
			}

			return null;
		}
	};
}
