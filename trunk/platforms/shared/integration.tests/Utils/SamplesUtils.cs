using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	/// <summary>
	/// Utililities that manage test samples, such as sample log files.
	/// Samples are stored on the web.
	/// </summary>
	public class SamplesUtils
	{
		private HttpClient httpClient;
		private readonly string cacheDir;

		public Uri RepositoryUrl { get; private set; }

		public SamplesUtils(): this(new Uri("https://publogjoint.blob.core.windows.net/samples/"))
		{ }

		public SamplesUtils(Uri repositoryUrl)
		{
			this.RepositoryUrl = repositoryUrl;
			this.cacheDir = Path.Combine(Path.GetTempPath(), "logjoint.int.tests.cache");
			Directory.CreateDirectory(cacheDir);
		}

		/// <summary>
		/// Downloads a sample from repository if not downloaded yet and return a local file path
		/// </summary>
		public async Task<string> GetSampleAsLocalFile(string sampleName)
		{
			var sampleCacheLocation = Path.Combine(cacheDir,
				$"{GetSampleNameHash(sampleName)}-{EscapeSampleName(sampleName)}");
			if (File.Exists(sampleCacheLocation))
			{
				return sampleCacheLocation;
			}

			if (httpClient == null)
				httpClient = new HttpClient();

			var sampleUri = GetSampleAsUri(sampleName);
			using (var rsp = await httpClient.GetAsync(sampleUri))
			{
				if (rsp.StatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new Exception($"Failed to download sample {sampleName} from {sampleUri}: {rsp.StatusCode}");
				}
				var content = await rsp.Content.ReadAsByteArrayAsync();
				File.WriteAllBytes(sampleCacheLocation, content);
			}

			return sampleCacheLocation;
		}

		public Uri GetSampleAsUri(string sampleName)
		{
			return new Uri(RepositoryUrl, sampleName);
		}


		public static string GetSampleNameHash(string sampleName)
		{
			return Hashing.GetStableHashCode(sampleName).ToString("x");
		}

		public static string EscapeSampleName(string sampleName)
		{
			return new string(sampleName.Select(
				c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c
			).ToArray());
		}
	};
}
