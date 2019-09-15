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

namespace LogJoint.Symphony.SpringServiceLog
{
	public class CloudWatchDownloader
	{
		public static async Task DownloadBackendLogs(WebViewTools.IWebViewTools webViewTools)
		{
			var region = RegionEndpoint.USEast1;
			var loginEntryPoint = "https://duo.symphony.com/dag/saml2/idp/SSOService.php?spentityid=DI1D2H726TCM30VUZSK7";
			var logGroupName = "sym-qa5-rtc";
			var logStreamNamePrefix = "qa-sym-qa5-cs/qa-sym-qa5-cs/";

			string samlAssertion = await GetSAMLAssertionFromUser(webViewTools, loginEntryPoint);
			using (var authCli = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), region))
			{
				var authReq = CreateAssumeWithSAMLRequest(samlAssertion);
				var authResponse = await authCli.AssumeRoleWithSAMLAsync(authReq);
				using (var logscli = new AmazonCloudWatchLogsClient(authResponse.Credentials, RegionEndpoint.USEast1))
				{
					var getLogs = logscli.FilterLogEvents(new FilterLogEventsRequest
					{
						StartTime = DateTime.Now.ToUnixTimestampMillis() - (long)TimeSpan.FromDays(4).TotalMilliseconds,
						EndTime = DateTime.Now.ToUnixTimestampMillis() - (long)TimeSpan.FromDays(1).TotalMilliseconds,
						Limit = 10,
						LogGroupName = logGroupName,
						LogStreamNamePrefix = logStreamNamePrefix,
						FilterPattern = "\"448a2e20-5fd7-4a9c-b52a-b75947d543fd\""
					});
					Console.WriteLine("Hello World");
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
	};
}
