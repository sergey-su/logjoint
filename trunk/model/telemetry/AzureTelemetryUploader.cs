using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Telemetry
{
	public class AzureTelemetryUploader: ITelemetryUploader
	{
		static readonly LJTraceSource trace = new LJTraceSource("Telemetry");
		readonly string telemetryUrl, issuesUrl;

		public AzureTelemetryUploader(
			string telemetryUrl,
			string issuesUrl
		)
		{
			this.telemetryUrl = telemetryUrl;
			if (!Uri.IsWellFormedUriString(this.telemetryUrl, UriKind.Absolute))
				this.telemetryUrl = null;
			this.issuesUrl = issuesUrl;
			if (!Uri.IsWellFormedUriString(this.issuesUrl, UriKind.Absolute))
				this.issuesUrl = null;
		}

		bool ITelemetryUploader.IsTelemetryConfigured
		{
			get { return telemetryUrl != null; }
		}
		
		bool ITelemetryUploader.IsIssuesReportingConfigured
		{
			get { return issuesUrl != null; }
		}

		async Task<TelemetryUploadResult> ITelemetryUploader.Upload(
			DateTime recordTimestamp,
			string recordId,
			Dictionary<string, string> fields,
			CancellationToken cancellation
		)
		{
			if (telemetryUrl == null)
				throw new InvalidOperationException("telemetry uploader is not initialized");
			var request = HttpWebRequest.CreateHttp(telemetryUrl);
			request.Method = "POST";
			request.ContentType = "application/atom+xml";
			request.Headers.Add("x-ms-version", "2014-02-14");
			request.Headers.Add("Prefer", "return-no-content");
			using (var requestStream = await request.GetRequestStreamAsync().WithCancellation(cancellation))
			{
				XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
				XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
				XNamespace a = "http://www.w3.org/2005/Atom";
				var requestXml = new XDocument(
					new XElement(
						a + "entry",
						new XAttribute("xmlns", a.NamespaceName),
						new XAttribute(XNamespace.Xmlns + "d", d.NamespaceName),
						new XAttribute(XNamespace.Xmlns + "m", m.NamespaceName),
						new XElement(a + "title"),
						new XElement(a + "updated", recordTimestamp.ToString("o")),
						new XElement(a + "author", new XElement("name")),
						new XElement(a + "id"),
						new XElement(
							a + "content",
							new XAttribute("type", "application/xml"),
							new XElement(
								m + "properties",
								new []
								{
									new XElement(d + "PartitionKey", recordTimestamp.ToString("s")), // PK = timestamp in sortable format
									new XElement(d + "RowKey", recordId), // RK = telemetry record ID
								}.Union(
									fields.Select(f => FieldToAttr(f, d, m))
								).ToArray()
							)
						)
					)
				);
				requestXml.Save(requestStream);
			}
			using (var response = (HttpWebResponse)await request.GetResponseNoException().WithCancellation(cancellation))
			{
				if (response.StatusCode == HttpStatusCode.NoContent
				 || response.StatusCode == HttpStatusCode.Created
				 || response.StatusCode == HttpStatusCode.OK)
					return TelemetryUploadResult.Success;
				if (response.StatusCode == HttpStatusCode.Conflict)
					return TelemetryUploadResult.Duplicate;
				using (var responseStream = response.GetResponseStream())
				using (var responseReader = new StreamReader(responseStream))
				{
					trace.Error("Failed to upload telemetry: {0}", await responseReader.ReadToEndAsync());
				}
				return TelemetryUploadResult.Failure;
			}
		}
		
		async Task<string> ITelemetryUploader.UploadIssueReport(
			Stream reportStream,
			CancellationToken cancellation
		)
		{
			if (issuesUrl == null)
				throw new InvalidOperationException("issues reporting is not initialized");
			var reportId = Guid.NewGuid().ToString("N");
			var requestUrl = issuesUrl.Insert(
				issuesUrl.IndexOf("?", StringComparison.Ordinal), "/" + reportId);
			var request = HttpWebRequest.CreateHttp(requestUrl);
			request.Method = "PUT";
			request.ContentType = "application/zip";
			request.Headers.Add("x-ms-blob-type", "BlockBlob");
			request.ContentLength = reportStream.Length;
			reportStream.Position = 0;
			using (var requestStream = await request.GetRequestStreamAsync().WithCancellation(cancellation))
			{
				await reportStream.CopyToAsync(requestStream);
			}
			using (var response = (HttpWebResponse)await request.GetResponseAsync().WithCancellation(cancellation))
			{
			}
			return reportId;
		}

		static XElement FieldToAttr(KeyValuePair<string, string> field, XNamespace d, XNamespace m)
		{
			var ret = new XElement(d + field.Key);
			if (field.Value == null)
				ret.Add(new XAttribute(m + "null", "true"));
			else
				ret.Add(field.Value);
			return ret;
		}
	}
}
