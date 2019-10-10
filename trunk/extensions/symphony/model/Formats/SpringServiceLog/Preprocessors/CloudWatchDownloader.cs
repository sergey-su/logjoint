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
using LogJoint.Postprocessing;
using System.Threading;

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
			public string SAMLRole { get; private set; }

			public override string ToString() => Name;

			public static readonly Environment QA5 = new Environment
			{
				Name = "qa5",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI1D2H726TCM30VUZSK7",
				LogGroupName = "sym-qa5-rtc",
				LogStreamNamePrefix = "qa-sym-qa5-cs/qa-sym-qa5-cs/",
				SAMLRole = "Sym-SSO-DUO-Dev-Standard-Role",
			};

			public static readonly Environment ST2 = new Environment
			{
				Name = "st2",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DIM6CNTQJPKJ6D4GJK04",
				LogGroupName = "sym-st2-rtc",
				LogStreamNamePrefix = "dev-sym-st2-cs/dev-sym-st2-cs/",
				SAMLRole = "Sym-SSO-DUO-Dev-Standard-Role",
			};

			public static readonly Environment RTC1 = new Environment
			{
				Name = "rtc1",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DIM6CNTQJPKJ6D4GJK04",
				LogGroupName = "sym-rtc1-rtc",
				LogStreamNamePrefix = "dev-sym-rtc1-cs/dev-sym-rtc1-cs/",
				SAMLRole = "Sym-SSO-DUO-Dev-Standard-Role",
			};

			public static readonly Environment Corporate = new Environment
			{
				Name = "corporate",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI17XYLYKQYHON337JU6",
				LogGroupName = "sym-corp-stage-chat-glb-1-ms",
				LogStreamNamePrefix = "rtc-cs/rtc-cs/",
				SAMLRole = "Sym-SSO-DUO-Dev-Standard-Role",
			};

			public static readonly Environment CitiUAT = new Environment
			{
				Name = "citi-test2",
				Region = RegionEndpoint.USEast1,
				LoginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI17XYLYKQYHON337JU6",
				LogGroupName = "uat-citi-na-2-rtc",
				LogStreamNamePrefix = "rtc-cs/rtc-cs/",
				SAMLRole = "Sym-SSO-DUO-Dev-Standard-Role",
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
				var downloadedEvents = new Dictionary<string, FilteredLogEvent>(); // event key -> event, used to dedupe events
				var downloadedIds = new HashSet<string>();
				var ids = new HashSet<string>(request.Ids);
				var ambientIds = new HashSet<string>();
				var beginDate = request.ReferenceTime - TimeSpan.FromHours(2);
				var endDate = request.ReferenceTime + TimeSpan.FromHours(2);
				var logGroupName = request.Env.LogGroupName;
				var logStreamNamePrefix = request.Env.LogStreamNamePrefix;

				string getKey(FilteredLogEvent e) => e.EventId;

				for (;;)
				{
					var currentIds =
						ids.Count > 0 ? ids :
						ambientIds.Count > 0 ? ambientIds :
						null;
					if (currentIds == null)
						break;

					List<FilteredLogEvent> batch;
					if (currentIds == ids)
					{
						batch = await DownloadBySubstrings(logscli, logGroupName, logStreamNamePrefix,
							beginDate.ToUnixTimestampMillis(), endDate.ToUnixTimestampMillis(),
							currentIds, statusReporter);
					}
					else
					{
						var exactBeginDate = downloadedEvents.Values.Aggregate(
							long.MaxValue,
							(curr, e) => Math.Min(curr, e.Timestamp)
						);
						var exactEndDate = downloadedEvents.Values.Aggregate(
							long.MinValue,
							(curr, e) => Math.Max(curr, e.Timestamp)
						);
						batch = await DownloadBySubstrings(logscli, logGroupName, logStreamNamePrefix,
							exactBeginDate, exactEndDate, ambientIds, statusReporter);
					}
					foreach (var id in currentIds)
					{
						downloadedIds.Add(id);
					}
					currentIds.Clear();
					foreach (var m in batch)
					{
						if (!downloadedEvents.ContainsKey(getKey(m)))
						{
							downloadedEvents.Add(getKey(m), m);
							foreach (var (id, isAmbient) in ExtractIds(m.Message))
							{
								if (!downloadedIds.Contains(id))
									(isAmbient ? ambientIds : ids).Add(id);
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
				return result.ToDictionary(
					i => i.Key,
					i => i.Value
						.Select(e => (e, ts: Reader.Read(e.Message)?.Timestamp))
						.Where(p => p.ts != null)
						.OrderBy(e => e.ts.Value)
						.Aggregate(
							new StringBuilder(),
							(builder, e) => builder.AppendLine(e.e.Message),
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
				var authReq = CreateAssumeWithSAMLRequest(samlAssertion, env.SAMLRole);
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

		private static AssumeRoleWithSAMLRequest CreateAssumeWithSAMLRequest(string samlAssertion, string expectedRoleSubstring)
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
						.Where(n => n.Name == XName.Get("Attribute", samlXmlns)
							&& n.Attribute("Name")?.Value == "https://aws.amazon.com/SAML/Attributes/Role")
						.SelectMany(n => n.Elements(XName.Get("AttributeValue", samlXmlns)).Select(valElement => valElement.Value))
						.ToList();
				if (roles.Count == 0)
				{
					throw new Exception("SAML assertion contains no roles");
				}
				var role = roles.FirstOrDefault(r => r.Contains(expectedRoleSubstring));
				if (role == null)
				{
					throw new Exception($"SAML assertion does contains the role having '{expectedRoleSubstring}'. All roles: {string.Join(",", roles)}");
				}
				var split = role.Split(',');
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
			HashSet<string> substrings,
			Action<string> statusReporter
		)
		{
			var result = new List<FilteredLogEvent>();
			async Task DownloadWithFilter(string filter, IReadOnlyList<string> filterSubstrings)
			{
				statusReporter($"Downloading {string.Join(", ", filterSubstrings.Take(3))}{(filterSubstrings.Count > 3 ? $" +{filterSubstrings.Count - 3} more" : "")}...");
				for (string continuationToken = null; substrings.Count > 0;)
				{
					var rsp = await cli.FilterLogEventsAsync(new FilterLogEventsRequest
					{
						NextToken = continuationToken,
						StartTime = beginDate,
						EndTime = endDate,
						LogGroupName = logGroupName,
						LogStreamNamePrefix = logStreamNamePrefix,
						FilterPattern = filter
					});
					result.AddRange(rsp.Events);
					if (rsp.NextToken == null)
						break;
					continuationToken = rsp.NextToken;
				}
			}
			var filterLenLimit = 1024;
			var filterBuilder = new StringBuilder();
			var filterSubstringsList = new List<string>();
			foreach (var s in substrings)
			{
				var term = $" ?\"{s}\"";
				if (term.Length > filterLenLimit)
					continue;
				if (filterBuilder.Length + term.Length > filterLenLimit)
				{
					await DownloadWithFilter(filterBuilder.ToString(), filterSubstringsList);
					filterBuilder.Clear();
					filterSubstringsList.Clear();
				}
				filterBuilder.Append(term);
				filterSubstringsList.Add(s);
			}
			if (filterBuilder.Length > 0)
				await DownloadWithFilter(filterBuilder.ToString(), filterSubstringsList);
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

		const string conferenceSessionIdRegex = @"(?<id_conf>[^\,]+)";
		const string sessionIdRegex = @"(?<id_session>[\w\-]+)";
		const string mixerIdRegex = @"(?<id_mixer>\w+)";
		private readonly static Regex joinRe = new Regex($@"handleJoin conferenceSessionId {conferenceSessionIdRegex}, sessionId {sessionIdRegex}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex eventRegex = new Regex($@"Sending event type: \w+, sessionId: {sessionIdRegex}, conferenceSessionId: {conferenceSessionIdRegex}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex requestRe = new Regex(@"Incoming request \[(?<id_request>[^\]]+)\]", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex mediaBridgeRe = new Regex($@"Acquired mediaBridgeSessionId (?<id_ambient_mbr>[\w\-]+), conferenceSessionId", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex sipBridgeRe = new Regex($@"Acquired sipBridgeSessionId (?<id_ambient_sipb>[\w\-]+), conferenceSessionId", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex mixerRe1 = new Regex($@"onMixerAllocResponse conferenceSessionId {conferenceSessionIdRegex} created mixer id {mixerIdRegex}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex mixerRe2 = new Regex($@"Multi-leg transport allocated on JBV for mixer {mixerIdRegex} in {conferenceSessionIdRegex}", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private readonly static Regex[] idRegexps =
		{
			joinRe,
			eventRegex,
			requestRe,
			mediaBridgeRe,
			sipBridgeRe,
			mixerRe1,
			mixerRe2
		};
	};
}
