using LogJoint.Analytics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public static class Converters
	{
		static Exception badDumpException = new ArgumentException("bad dump");
		static Regex statsEntryNameRe = new Regex(@"^(?<objId>.+?)\-(?<prop>\w+)$");

		public static async Task JsonToLog(string jsonFileName, string outputFile)
		{
			// O(n)-memory converter. Possible optimization: use streaming json reader.
			var outputMessages = new List<Message>();
			using (var reader = new StreamReader(jsonFileName))
				JsonToLog(JToken.Parse(await reader.ReadToEndAsync()) as JObject, outputMessages);
		}

		public static void JsonToLog(JObject root, List<Message> output)
		{
			if (root == null)
				throw badDumpException;
			var conns = root.Property("PeerConnections")?.Value as JObject;
			if (conns == null)
				throw badDumpException;
			foreach (var conn in conns.Properties())
			{
				HandlePeerConnection(conn.Name, conn.Value as JObject, output);
			}
		}

		static void HandlePeerConnection(string connName, JObject connJson, List<Message> output)
		{
			if (connJson == null)
				throw badDumpException;
			var stats = connJson.Property("stats")?.Value as JObject;
			if (stats != null)
			{
				foreach (var statEntry in stats.Properties())
				{
					var entryMatch = statsEntryNameRe.Match(statEntry.Name);
					if (!entryMatch.Success)
						continue;

				}
			}
		}
	}
}
