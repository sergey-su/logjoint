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
	public static class CloudWatchDownloader
	{
		public class Environment
		{
			public string Name { get; private set; }
			public RegionEndpoint Region { get; private set; }
			public string LoginEntryPoint { get; private set; }
			public string LogGroupName { get; private set; }
			public string LogStreamNamePrefix { get; private set; }

			public override string ToString() => Name;

			public static readonly Environment QA5 = new Environment
			{
				Name = "qa5",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI1D2H726TCM30VUZSK7",
				LogGroupName = "sym-qa5-rtc",
				LogStreamNamePrefix = "qa-sym-qa5-cs/qa-sym-qa5-cs/",
			};

			public static readonly Environment ST2 = new Environment
			{
				Name = "st2",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DIM6CNTQJPKJ6D4GJK04",
				LogGroupName = "sym-st2-rtc",
				LogStreamNamePrefix = "dev-sym-st2-cs/dev-sym-st2-cs/",
			};

			public static readonly Environment RTC1 = new Environment
			{
				Name = "rtc1",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DIM6CNTQJPKJ6D4GJK04",
				LogGroupName = "sym-rtc1-rtc",
				LogStreamNamePrefix = "dev-sym-rtc1-cs/dev-sym-rtc1-cs/",
			};

			public static readonly Environment Corporate = new Environment
			{
				Name = "corporate",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI17XYLYKQYHON337JU6",
				LogGroupName = "sym-corp-stage-chat-glb-1-ms",
				LogStreamNamePrefix = "rtc-cs/rtc-cs/",
			};

			public static readonly Environment CitiUAT = new Environment
			{
				Name = "citi-test2",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI17XYLYKQYHON337JU6",
				LogGroupName = "uat-citi-na-2-rtc",
				LogStreamNamePrefix = "rtc-cs/rtc-cs/",
			};

			public static readonly IReadOnlyDictionary<string, Environment> Environments = new []
			{
				QA5,
				ST2,
				RTC1,
				Corporate,
				CitiUAT
			}.ToDictionary(env => env.Name);
		};

		public class DownloadRequest
		{
			public Environment Env { get; private set; }
			public IReadOnlyCollection<string> Ids { get; private set; }
			public DateTime ReferenceTime { get; private set; }

			public DownloadRequest(string env, IReadOnlyCollection<string> ids, DateTime referenceTime)
			{
				if (!Environment.Environments.TryGetValue(env ?? throw new ArgumentNullException(nameof(env)), out var tmpEnv))
					throw new ArgumentException($"Unknown environment '{env}'", nameof(env));
				else
					Env = tmpEnv;
				Ids = ids ?? throw new ArgumentNullException(nameof(ids));
				if (Ids.Count == 0)
					throw new ArgumentException("No ids provided to logs downloader");
				ReferenceTime = referenceTime;
			}
		};

		public static async Task<Dictionary<string, string>> Download(
			WebViewTools.IWebViewTools webViewTools,
			DownloadRequest request,
			Action<string> statusReporter
		)
		{
			statusReporter("Authenticating...");
			using (var logscli = await CreateLogsClient(webViewTools, request.Env))
			{
				var downloadedEvents = new Dictionary<string, FilteredLogEvent>(); // id -> event, used to dedupe events
				var downloadedIds = new Dictionary<string, DateRange>(); // id -> range downloaded TODO: remove range?
				var ambientIds = new HashSet<string>();
				var currBeginDate = request.ReferenceTime - TimeSpan.FromHours(2);
				var currEndDate = request.ReferenceTime + TimeSpan.FromHours(2);
				var logGroupName = request.Env.LogGroupName;
				var logStreamNamePrefix = request.Env.LogStreamNamePrefix;
				var newIds = new HashSet<string>(request.Ids);

				string getKey(FilteredLogEvent e) => e.EventId;

				while (newIds.Count > 0)
				{
					statusReporter($"Downloading {string.Join(", ", newIds.Take(3))}{(newIds.Count > 3 ? $" +{newIds.Count - 3} more" : "")}...");

					var batch = await DownloadBySubstrings(logscli, logGroupName, logStreamNamePrefix,
						currBeginDate.ToUnixTimestampMillis(), currEndDate.ToUnixTimestampMillis(), newIds);
					foreach (var id in newIds)
					{
						downloadedIds.Add(id, new DateRange(currBeginDate, currEndDate));
					}
					newIds.Clear();
					foreach (var m in batch)
					{
						if (!downloadedEvents.ContainsKey(getKey(m)))
						{
							downloadedEvents.Add(getKey(m), m);
							foreach (var (id, isAmbient) in ExtractIds(m.Message))
							{
								if (isAmbient)
									ambientIds.Add(id);
								else if (!downloadedIds.ContainsKey(id))
									newIds.Add(id);
							}
						}
					}
				}

				var exactBeginDate = downloadedEvents.Values.Aggregate(
					long.MaxValue,
					(curr, e) => Math.Min(curr, e.Timestamp)
				);
				var exactEndDate = downloadedEvents.Values.Aggregate(
					long.MinValue,
					(curr, e) => Math.Max(curr, e.Timestamp)
				);
				foreach (var m in await DownloadBySubstrings(logscli, logGroupName, logStreamNamePrefix,
					exactBeginDate, exactEndDate, ambientIds))
				{
					if (!downloadedEvents.ContainsKey(getKey(m)))
						downloadedEvents.Add(getKey(m), m);
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
				return result.ToDictionary(
					i => i.Key,
					i => i.Value.OrderBy(e => e.Timestamp).Aggregate(
						new StringBuilder(),
						(builder, e) => builder.AppendLine(e.Message),
						builder => builder.ToString()
					)
				);
			}
		}

		private static async Task<AmazonCloudWatchLogsClient> CreateLogsClient(WebViewTools.IWebViewTools webViewTools, Environment env)
		{
			string samlAssertion = await GetSAMLAssertionFromUser(webViewTools, env.LoginEntryPoint);
			using (var authCli = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), env.Region))
			{
				var authReq = CreateAssumeWithSAMLRequest(samlAssertion);
				var authResponse = await authCli.AssumeRoleWithSAMLAsync(authReq);
				return new AmazonCloudWatchLogsClient(authResponse.Credentials, env.Region);
			}
		}

		private static async Task<string> GetSAMLAssertionFromUser(
			WebViewTools.IWebViewTools webViewTools, string loginEntryPoint)
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

		private static async Task<List<FilteredLogEvent>> DownloadBySubstrings(
			AmazonCloudWatchLogsClient cli,
			string logGroupName,
			string logStreamNamePrefix,
			long beginDate,
			long endDate,
			HashSet<string> substrings
		)
		{
			var result = new List<FilteredLogEvent>();
			for (string continuationToken = null; substrings.Count > 0;)
			{
				var rsp = await cli.FilterLogEventsAsync(new FilterLogEventsRequest
				{
					NextToken = continuationToken,
					StartTime = beginDate,
					EndTime = endDate,
					LogGroupName = logGroupName,
					LogStreamNamePrefix = logStreamNamePrefix,
					FilterPattern = string.Join(" ", substrings.Select(id => $"?\"{id}\"")) // todo: escape " in id
				});
				result.AddRange(rsp.Events);
				if (rsp.NextToken == null)
					break;
				continuationToken = rsp.NextToken;
			}
			return result;
		}

		private static IEnumerable<(string value, bool isAmbient)> ExtractIds(string logLine)
		{
			foreach (var re in idRegexps)
			{
				var m = re.Match(logLine);
				if (m.Success)
				{
					for (int g = 1; g < m.Groups.Count; ++g)
					{
						yield return (m.Groups[g].Value, re.GetGroupNames()[g].Contains("ambient"));
					}
				}
			}
		}

		private readonly static Regex joinRe = new Regex(@"handleJoin conferenceSessionId (?<id_conf>[^\,]+), sessionId (?<id_session>[\w\-]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex requestRe = new Regex(@"Incoming request \[(?<id_request>[^\]]+)\]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex mediaBridgeRe = new Regex($@"Acquired mediaBridgeSessionId (?<id_ambient_mbr>[\w\-]+), conferenceSessionId (?<id_conf>[^\,]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex sipBridgeRe = new Regex($@"Acquired sipBridgeSessionId (?<id_ambient_sipb>[\w\-]+), conferenceSessionId (?<id_conf>[^\,]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex[] idRegexps =
		{
			joinRe,
			requestRe,
			mediaBridgeRe,
			sipBridgeRe,
		};
	};
}
