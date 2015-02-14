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
	}
}
