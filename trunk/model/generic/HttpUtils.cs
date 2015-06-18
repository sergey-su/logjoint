using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint
{
	public static class HttpUtils
	{
		public static async Task<HttpWebResponse> GetResponseNoException(this HttpWebRequest req)
		{
			try
			{
				return (HttpWebResponse)await req.GetResponseAsync();
			}
			catch (WebException we)
			{
				var resp = we.Response as HttpWebResponse;
				if (resp == null)
					throw;
				return resp;
			}
		}

		public static async Task LogAndThrowOnFailure(this HttpWebResponse rsp, LJTraceSource trace)
		{
			if ((int)rsp.StatusCode >= 200 && (int)rsp.StatusCode < 300)
				return;
			using (var responseStream = rsp.GetResponseStream())
			using (var responseReader = new StreamReader(responseStream))
			{
				trace.Error("http failed. {0} {1} {2}", 
					rsp.Method, rsp.ResponseUri, await responseReader.ReadToEndAsync());
			}
			throw new WebException("http request failed");
		}
	}
}
