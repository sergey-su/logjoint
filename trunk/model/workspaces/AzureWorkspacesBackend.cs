using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LogJoint.Workspaces.Backend
{
	class AzureWorkspacesBackend: IBackendAccess
	{
		readonly LJTraceSource trace;
		readonly Uri serviceUrl;
		readonly Lazy<XmlSerializer> wsSerializer = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(WorkspaceDTO)));
		readonly Lazy<XmlSerializer> createdWsSerializer = new Lazy<XmlSerializer>(() => new XmlSerializer(typeof(CreatedWorkspaceDTO)));
		readonly XmlWriterSettings wsWriterSettings = new XmlWriterSettings() { NewLineHandling = NewLineHandling.Entitize };
		readonly XmlReaderSettings wsReaderSettings = new XmlReaderSettings() { IgnoreWhitespace = false };

		public AzureWorkspacesBackend(ITraceSourceFactory traceSourceFactory, string configUri)
		{
			this.trace = traceSourceFactory.CreateTraceSource("Workspaces", "wsbackend");
			if (Uri.IsWellFormedUriString(configUri, UriKind.Absolute))
				this.serviceUrl = new Uri(configUri);
		}

		bool IBackendAccess.IsConfigured
		{
			get { return serviceUrl != null; }
		}

		bool IBackendAccess.IsValidWorkspaceUri(Uri uri)
		{
			return Uri.Compare(uri, serviceUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
		}

		async Task<CreatedWorkspaceDTO> IBackendAccess.CreateWorkspace(WorkspaceDTO dto)
		{
			var request = HttpWebRequest.CreateHttp(serviceUrl);
			request.Method = "POST";
			request.ContentType = request.Accept = "application/xml";
			using (var requestStream = await request.GetRequestStreamAsync())
			using (var xmlWriter = XmlWriter.Create(requestStream, wsWriterSettings))
			{
				wsSerializer.Value.Serialize(xmlWriter, dto);
			}
			using (var response = (HttpWebResponse)await request.GetResponseNoException())
			{
				await response.LogAndThrowOnFailure(trace);
				using (var responseStream = response.GetResponseStream())
				{
					return (CreatedWorkspaceDTO)createdWsSerializer.Value.Deserialize(responseStream);
				}
			}
		}

		async Task IBackendAccess.UploadEntriesArchive(string destinationRef, Stream source)
		{
			var request = HttpWebRequest.CreateHttp(destinationRef);
			request.Method = "POST";
			using (var requestStream = await request.GetRequestStreamAsync())
			{
				await source.CopyToAsync(requestStream);
			}
			using (var response = (HttpWebResponse)await request.GetResponseNoException())
			{
				await response.LogAndThrowOnFailure(trace);
			}
		}


		async Task<WorkspaceDTO> IBackendAccess.GetWorkspace(string workspaceUri, CancellationToken cancellation)
		{
			var request = HttpWebRequest.CreateHttp(workspaceUri);
			request.Method = "GET";
			request.Accept = "application/xml";
			using (var response = (HttpWebResponse)await request.GetResponseNoException().WithCancellation(cancellation))
			{
				await response.LogAndThrowOnFailure(trace).WithCancellation(cancellation);
				using (var responseStream = response.GetResponseStream())
				using (var responseStreamReader = new StreamReader(responseStream, Encoding.UTF8))
				using (var responseXmlReader = XmlReader.Create(responseStreamReader, wsReaderSettings))
				{
					return (WorkspaceDTO)wsSerializer.Value.Deserialize(responseXmlReader);
				}
			}

		}

		async Task IBackendAccess.GetEntriesArchive(string uri, Stream destinationStream, CancellationToken cancellation)
		{
			var request = HttpWebRequest.CreateHttp(uri);
			request.Method = "GET";
			using (var response = (HttpWebResponse)await request.GetResponseNoException().WithCancellation(cancellation))
			{
				await response.LogAndThrowOnFailure(trace).WithCancellation(cancellation);
				using (var responseStream = response.GetResponseStream())
				{
					await responseStream.CopyToAsync(destinationStream, 4000, cancellation);
				}
			}
		}
	}
}
