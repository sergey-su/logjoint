using LogJoint.Analytics;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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
			var getUserMedias = root.Property("getUserMedia")?.Value as JArray;
			DateTime? firstTs = output.FirstOrDefault()?.Timestamp;
			if (firstTs != null && getUserMedias != null)
			{
				int callIdx = 0;
				foreach (var getUserMedia in getUserMedias.OfType<JObject>())
				{
					foreach (var prop in getUserMedia.Properties())
					{
						output.Add(new Message(0, 0, firstTs.Value, new StringSlice("M"), new StringSlice((callIdx + 1).ToString()),
							new StringSlice(""), new StringSlice(prop.Name), new StringSlice(prop.Value?.ToString() ?? ""), StringSlice.Empty));
					}
				}
			}

			output.Sort((m1, m2) => 
			{
				int cmp = m1.Timestamp.CompareTo(m2.Timestamp);
				if (cmp != 0)
					return cmp;
				return m1.Index - m2.Index;
			});
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
			DateTime? firstTs = output.FirstOrDefault()?.Timestamp;
			if (firstTs != null)
			{
				foreach (var staticProp in new[] { "constraints", "rtcConfiguration", "url" })
				{
					var val = connJson.Property(staticProp)?.Value as JValue;
					if (val != null)
						output.Add(new Message(0, 0, firstTs.Value, new StringSlice("C"), new StringSlice(connName),
							StringSlice.Empty, new StringSlice(staticProp), new StringSlice(val.ToString()), StringSlice.Empty));
				}
			}
			var updateLog = connJson.Property("updateLog")?.Value as JArray;
			if (firstTs != null && updateLog != null)
			{
				TimeSpan? tzOffesetGuess = null;
				int logEntryIdx = 0;
				foreach (var entry in updateLog.OfType<JObject>())
				{
					var timeStr = entry.GetValue("time")?.ToString();
					var type = entry.GetValue("type")?.ToString();
					var value = entry.GetValue("value")?.ToString();
					if (timeStr == null || type == null || value == null)
						continue;
					DateTime time;
					if (!DateTime.TryParseExact(timeStr, "dd'/'MM'/'yyyy', 'hh':'mm':'ss",
						System.Globalization.CultureInfo.InvariantCulture, 
						System.Globalization.DateTimeStyles.None, out time))
						continue;
					time = time.ToUnspecifiedTime();
					if (tzOffesetGuess == null)
					{
						var diff = (time - firstTs.Value).TotalHours;
						var roundedDiff = Math.Round(diff, 0);
						tzOffesetGuess = TimeSpan.FromHours(-roundedDiff);
					}
					time = time.Add(tzOffesetGuess.Value);
					output.Add(new Message(logEntryIdx++, 0, time, new StringSlice("C"), new StringSlice(connName),
						new StringSlice("log"), new StringSlice(type), new StringSlice(value), StringSlice.Empty));
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
			startTime = startTime.Value.ToUnspecifiedTime();
			endTime = endTime.Value.ToUnspecifiedTime();
			var step = new TimeSpan((endTime.Value - startTime.Value).Ticks / values.Count);
			var dt = startTime.Value;
			var isNumeric = values[0].Type == JTokenType.Float || values[0].Type == JTokenType.Integer;
			string prev = null;
			Message prevMsg = null;
			for (var i = 0; i < values.Count; ++i, dt = dt.Add(step))
			{
				var val = values[i].ToString();
				var msg = new Message(0, 0, dt, new StringSlice("C"), new StringSlice(rootObjectId),
					new StringSlice(objectId), new StringSlice(propName), new StringSlice(val), StringSlice.Empty);
				if (i == 0)
				{
					// always add first
					output.Add(msg);
				}
				else if (isNumeric)
				{
					if (val != prev)
					{
						if (output.Last() != prevMsg)
							output.Add(prevMsg);
						output.Add(msg);
					}
				}
				else
				{
					if (val != prev)
						output.Add(msg);
				}
				prev = val;
				prevMsg = msg;
			}
			if (prevMsg != null && output.Last() != prevMsg)
			{
				// always add last
				output.Add(prevMsg);
			}
		}
	}
}
