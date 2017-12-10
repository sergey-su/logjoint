using LogJoint.Analytics.Correlation;
using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Chromium.Correlation
{
	public class NodeDetectionToken : ISameNodeDetectionToken
	{
		readonly HashSet<uint> processIds;
		readonly Dictionary<string, ICECandidateInfo> iceCandidates;

		public NodeDetectionToken(IEnumerable<uint> processIds, IEnumerable<ICECandidateInfo> iceCandidates)
		{
			this.processIds = processIds.ToHashSet();
			this.iceCandidates = iceCandidates.ToDictionary(c => c.Id);
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
			return null;
		}
	};
}
