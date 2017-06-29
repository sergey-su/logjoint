using LogJoint.Analytics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
			outputMessages.Sort((m1, m2) => m1.Timestamp.CompareTo(m2.Timestamp));
			IWriter writer = new Writer();
			await writer.Write(() => new FileStream(outputFile, FileMode.Create), s => s.Dispose(), 
				new[] { outputMessages.ToArray() }.ToAsync());
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
					HandleStatEntry(connName, entryMatch.Groups[1].Value, entryMatch.Groups[2].Value, statEntry.Value as JObject, output);
				}
			}
		}

		static void HandleStatEntry(string rootObjectId, string objectId, string propName, JObject entryJson, List<Message> output)
		{
			if (entryJson == null)
				return;
			var startTime = (entryJson.Property("startTime")?.Value as JValue)?.Value as DateTime?;
			var endTime = (entryJson.Property("endTime")?.Value as JValue)?.Value as DateTime?;
			var values = JToken.Parse(entryJson.Property("values")?.Value?.ToString() ?? "[]") as JArray;
			if (startTime == null || endTime == null || values == null || values.Count == 0)
				return;
			if (startTime.Value > endTime.Value)
				return;
			var step = new TimeSpan((endTime.Value - startTime.Value).Ticks / values.Count);
			var dt = startTime.Value;
			var isNumeric = values[0].Type == JTokenType.Float;
			string oldVal = null;
			for (var i = 0; i < values.Count; ++i, dt = dt.Add(step))
			{
				var val = values[i].ToString();
				if (isNumeric || val != oldVal)
					output.Add(new Message(0, 0, dt, new StringSlice("C"), new StringSlice(rootObjectId),
						new StringSlice(objectId), new StringSlice(propName), new StringSlice(val), new StringSlice()));
				oldVal = val;
			}
		}
	}
}
