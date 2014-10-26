using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace LogJoint
{
	static class UrlDragDropUtils
	{
		public static bool IsUriDataPresent(IDataObject dataObj)
		{
			foreach (string format in Formats)
				if (dataObj.GetDataPresent(format))
					return true;
			return false;
		}

		public static IEnumerable<string> GetURLs(IDataObject dataObj)
		{
			return
				(from rawUrl in GetRawURLs(dataObj)
				 let url = ValidateURL(rawUrl)
				 where url != null
				 select url).Take(1);
		}

		static readonly string[] Formats = new string[] {
				"UniformResourceLocatorW",
				"UniformResourceLocator",
				"text/x-moz-url",
			};

		static string ValidateURL(string url)
		{
			if (url != null)
			{
				url = url.Trim();
				if (url.Length == 0)
					url = null;
			}
			return url;
		}

		static IEnumerable<string> GetRawURLs(IDataObject dataObj)
		{
			foreach (string format in Formats)
			{
				if (dataObj.GetDataPresent(format))
				{
					MemoryStream dataStream = dataObj.GetData(format) as MemoryStream;
					if (dataStream == null)
						continue;
					byte[] data = dataStream.ToArray();
					switch (format)
					{
						case "UniformResourceLocatorW":
							yield return Encoding.Unicode.GetString(data).TrimEnd('\0');
							break;
						case "UniformResourceLocator":
							yield return Encoding.ASCII.GetString(data).TrimEnd('\0');
							break;
						case "text/x-moz-url":
							yield return Encoding.Unicode.GetString(data).Split('\r', '\n').FirstOrDefault();
							break;
					}
				}
			}
		}
	};
}
