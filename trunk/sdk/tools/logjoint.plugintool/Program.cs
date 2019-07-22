using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

namespace LogJoint.PluginTool
{
	class Program
	{
		static readonly string PackageFileName = "plugin.zip";

		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("logjoint.updatetool.exe <command>");
				Console.WriteLine("commands:");
				Console.WriteLine("  pack <path to manifest.xml>                collects plug-in files referenced in the manifest and zips them");
				Console.WriteLine("  deploy <inbox url>                         sends the zip file with plug-in binaries into the plug-ins inbox");
				return 0;
			}

			switch (args[0])
			{
				case "pack":
					return Pack(args.Skip(1).ToArray());
				case "deploy":
					return Deploy(args.Skip(1).ToArray());
				default:
					Console.WriteLine("Unknown command");
					return 1;
			}
		}

		static int Pack(string[] args)
		{
			var manifestFileName = args.ElementAtOrDefault(0);
			if (string.IsNullOrEmpty(manifestFileName))
			{
				Console.WriteLine("Manifest not specified");
				return 1;
			}

			var binariesRoot = Path.GetDirectoryName(manifestFileName);
			var manifest = XDocument.Load(manifestFileName);
			var outputFileName = Path.GetFullPath(PackageFileName);

			using (var stream = new FileStream(outputFileName, FileMode.Create))
			using (var outputZip = new ZipArchive(stream, ZipArchiveMode.Create))
			{
				foreach (var fileElement in manifest.Elements("manifest").Elements("file"))
				{
					var filePath = Path.Combine(binariesRoot, fileElement.Value);
					Console.WriteLine("Adding {0}", filePath);
					outputZip.CreateEntryFromFile(filePath, fileElement.Value);
				}
				Console.WriteLine("Adding manifest {0}", manifestFileName);
				outputZip.CreateEntryFromFile(manifestFileName, "manifest.xml");
			}

			Console.WriteLine("Created successfully {0}", outputFileName);
			return 0;
		}

		static int Deploy(string[] args)
		{
			var inboxUrl = args.ElementAtOrDefault(0);
			if (string.IsNullOrEmpty(inboxUrl))
			{
				Console.WriteLine("Url is not specified");
				return 1;
			}
			Console.WriteLine("Url: {0}", inboxUrl);
			var client = new HttpClient();
			using (var zipStream = new FileStream(PackageFileName, FileMode.Open))
			using (var content = new StreamContent(zipStream))
			{
				content.Headers.Add("x-ms-blob-type", "BlockBlob");
				var response = client.PutAsync(new Uri(inboxUrl), content).Result;
				if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
				{
					Console.WriteLine("Publishing request failed: {0} {1}", response.StatusCode, response.ReasonPhrase);
					Console.WriteLine(response.Content.ReadAsStringAsync().Result);
					return 1;
				}
			}
			Console.WriteLine("Deployed successfully");
			return 0;
		}
	}
}
