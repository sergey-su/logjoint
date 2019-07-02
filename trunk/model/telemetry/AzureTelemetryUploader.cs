using Newtonsoft.Json;
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
		readonly LJTraceSource trace = new LJTraceSource("Telemetry");
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
			request.ContentType = "application/json";
			request.Accept = "application/json;odata=fullmetadata";
			request.Headers.Add("x-ms-version", "2016-05-31");
			//request.Headers.Add("Prefer", "return-no-content");
			using (var requestStream = await request.GetRequestStreamAsync().WithCancellation(cancellation))
			using (var requestWriter = new StreamWriter(requestStream))
			{
				JsonSerializer.CreateDefault().Serialize(requestWriter, new Dictionary<string, string>()
				{
					{ "PartitionKey", recordTimestamp.ToString("s") }, // PK = timestamp in sortable format
					{ "RowKey", recordId }, // RK = telemetry record ID
				}.Union(fields).ToDictionary(r => r.Key, r => r.Value));
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
