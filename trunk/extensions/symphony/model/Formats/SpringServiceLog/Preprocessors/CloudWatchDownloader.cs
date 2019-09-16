using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class CloudWatchDownloader
	{
		public static async Task<Dictionary<string, string>> DownloadBackendLogs(WebViewTools.IWebViewTools webViewTools)
		{
			var region = RegionEndpoint.USEast1; // todo: map from env
			var loginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI1D2H726TCM30VUZSK7"; // todo: map from env

			string samlAssertion = await GetSAMLAssertionFromUser(webViewTools, loginEntryPoint);
			using (var authCli = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), region))
			{
				var authReq = CreateAssumeWithSAMLRequest(samlAssertion);
				var authResponse = await authCli.AssumeRoleWithSAMLAsync(authReq);
				using (var logscli = new AmazonCloudWatchLogsClient(authResponse.Credentials, region))
				{
					return await F(logscli);
				}
			}
		}

		private static async Task<string> GetSAMLAssertionFromUser(WebViewTools.IWebViewTools webViewTools, string loginEntryPoint)
		{
			var samlForm = await webViewTools.UploadForm(new WebViewTools.UploadFormParams
			{
				FormUri = new Uri("https://signin.aws.amazon.com/saml"),
				Location = new Uri(loginEntryPoint)
			});
			var samlAssertion = samlForm.Values
				.Where(f => f.Key == "SAMLResponse")
				.Select(f => f.Value).FirstOrDefault();
			if (samlAssertion == null)
			{
				throw new Exception("No SAML assertion in login form");
			}

			return samlAssertion;
		}

		private static AssumeRoleWithSAMLRequest CreateAssumeWithSAMLRequest(string samlAssertion)
		{
			var authReq = new AssumeRoleWithSAMLRequest
			{
				SAMLAssertion = samlAssertion
			};
			using (var assertionStream = new MemoryStream(Convert.FromBase64String(samlAssertion)))
			{
				var assertionDoc = XDocument.Load(assertionStream);
				var samlXmlns = "urn:oasis:names:tc:SAML:2.0:assertion";
				var roles = assertionDoc
						.Descendants()
						.Where(n =>
								n.Name == XName.Get("Attribute", samlXmlns)
						 && n.Attribute("Name")?.Value == "https://aws.amazon.com/SAML/Attributes/Role")
						.Select(n => n.Element(XName.Get("AttributeValue", samlXmlns))?.Value)
						.ToList();
				if (roles.Count == 0)
				{
					throw new Exception("SAML assertion contains no roles");
				}
				if (roles.Count > 1)
				{
					throw new Exception("SAML assertion contains multiple roles");
				}
				var split = roles[0].Split(',');
				authReq.RoleArn = split[0];
				authReq.PrincipalArn = split[1];
			}
			return authReq;
		}

		private static async Task<Dictionary<string, string>> F(AmazonCloudWatchLogsClient cli)
		{
			var downloadedEvents = new Dictionary<string, FilteredLogEvent>(); // id -> event, used to dedupe events
			var downloadedIds = new Dictionary<string, DateRange>(); // id -> range downloaded
			var currBeginDate = DateTime.Now - TimeSpan.FromDays(4); // todo: initial date +- extra
			var currEndDate = DateTime.Now - TimeSpan.FromDays(1);
			var logGroupName = "sym-qa5-rtc"; // todo: map from env
			var logStreamNamePrefix = "qa-sym-qa5-cs/qa-sym-qa5-cs/"; // map from env
			var newIds = new HashSet<string>();
			newIds.Add("448a2e20-5fd7-4a9c-b52a-b75947d543fd"); // todo: initial ids
			while (newIds.Count > 0)
			{
				var batch = await DownloadByIds(cli, logGroupName, logStreamNamePrefix,
					currBeginDate, currEndDate, newIds);
				foreach (var id in newIds)
				{
					downloadedIds.Add(id, new DateRange(currBeginDate, currEndDate));
				}
				newIds.Clear();
				foreach (var m in batch)
				{
					var key = m.EventId; // todo: check if event id is unique
					if (!downloadedEvents.ContainsKey(key))
					{
						downloadedEvents.Add(key, m);
						foreach (var id in ExtractIds(m.Message))
						{
							if (!downloadedIds.ContainsKey(id))
								newIds.Add(id);
						}
					}
				}

			}
			var result = new Dictionary<string, List<FilteredLogEvent>>();
			foreach (var e in downloadedEvents.Values)
			{
				var roleInstanceId = e.LogStreamName;
				if (!result.TryGetValue(roleInstanceId, out var list))
				{
					result.Add(roleInstanceId, list = new List<FilteredLogEvent>());
				}
				list.Add(e);
			}
			foreach (var events in result.Values)
			{
				events.Sort((e1, e2) => Math.Sign(e1.Timestamp - e2.Timestamp));
			}
			return result.ToDictionary(
				i => i.Key,
				i => i.Value.Aggregate(new StringBuilder(), (builder, e) => builder.AppendLine(e.Message), builder => builder.ToString())
			);
		}

		private static async Task<List<FilteredLogEvent>> DownloadByIds(
			AmazonCloudWatchLogsClient cli,
			string logGroupName,
			string logStreamNamePrefix,
			DateTime beginDate,
			DateTime endDate,
			HashSet<string> ids
		)
		{
			var result = new List<FilteredLogEvent>();
			foreach (string id in ids)
			{
				// todo: query all ids at once
				for (string continuationToken = null;;)
				{
					var rsp = await cli.FilterLogEventsAsync(new FilterLogEventsRequest
					{
						NextToken = continuationToken,
						StartTime = beginDate.ToUnixTimestampMillis(),
						EndTime = endDate.ToUnixTimestampMillis(),
						LogGroupName = logGroupName,
						LogStreamNamePrefix = logStreamNamePrefix,
						FilterPattern = $"\"{id}\""
					});
					result.AddRange(rsp.Events);
					if (rsp.NextToken == null)
						break;
					continuationToken = rsp.NextToken;
				}
			}
			return result;
		}

		private static IEnumerable<string> ExtractIds(string logLine)
		{
			foreach (var re in idRegexps)
			{
				var m = re.Match(logLine);
				if (m.Success)
				{
					for (int g = 1; g < m.Groups.Count; ++g)
					{
						yield return m.Groups[g].Value;
					}
				}
			}
		}

		private readonly static Regex idRe1 = new Regex(@"handleJoin conferenceSessionId (?<id1>[^\,]+), sessionId (?<id2>[\w\-]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex[] idRegexps =
		{
			idRe1
		};
	};
}
