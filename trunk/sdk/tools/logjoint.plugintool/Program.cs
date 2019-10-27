using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.PluginTool
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("logjoint.updatetool.exe <command>");
				Console.WriteLine("commands:");
				Console.WriteLine("  pack <path to manifest.xml> <zip-name> [prod]       Collects plug-in files referenced in the manifest and zips them into a deployable package.");
				Console.WriteLine("                                                      Once package is deployed, if prod flag is not specified, the package will be verified, but not published to users.");
				Console.WriteLine("  deploy <zip-name> <inbox url>                       Sends the plug-in package into the plug-ins inbox.");
				Console.WriteLine("  test <path to manifest.xml> <host app directory>    Runs plug-in's integration tests");
				return 0;
			}

			switch (args[0])
			{
				case "pack":
					return Pack(args.Skip(1).ToArray());
				case "deploy":
					return Deploy(args.Skip(1).ToArray());
				case "test":
					return Test(args.Skip(1).ToArray());
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
			var zipFileName = args.ElementAtOrDefault(1);
			if (string.IsNullOrEmpty(zipFileName))
			{
				Console.WriteLine("Zip file name not specified");
				return 1;
			}
			var prod = args.ElementAtOrDefault(2) == "prod";

			var binariesRoot = Path.GetDirectoryName(manifestFileName);
			var manifest = XDocument.Load(manifestFileName);
			var outputFileName = Path.GetFullPath(zipFileName);

			using (var stream = new FileStream(outputFileName, FileMode.Create))
			using (var outputZip = new ZipArchive(stream, ZipArchiveMode.Create))
			{
				foreach (var fileElement in manifest.Elements("manifest").Elements("file"))
				{
					var filePath = Path.Combine(binariesRoot, fileElement.Value);
					Console.WriteLine("Adding {0}", filePath);
					outputZip.CreateEntryFromFile(filePath, fileElement.Value);
				}

				Console.WriteLine("Adding manifest {0} ({1})", manifestFileName, prod ? "PRODUCTION" : "STAGING");

				if (prod)
					manifest.Root.SetAttributeValue("production", "true");
				var tempManifestFileName = Path.GetTempFileName();
				manifest.Save(tempManifestFileName);

				outputZip.CreateEntryFromFile(tempManifestFileName, "manifest.xml");

				File.Delete(tempManifestFileName);
			}

			Console.WriteLine("Created successfully {0}", outputFileName);
			return 0;
		}

		static int Deploy(string[] args)
		{
			var zipFileName = args.ElementAtOrDefault(0);
			if (string.IsNullOrEmpty(zipFileName))
			{
				Console.WriteLine("Zip file name is not specified");
				return 1;
			}
			var inboxUrl = args.ElementAtOrDefault(1);
			if (string.IsNullOrEmpty(inboxUrl))
			{
				Console.WriteLine("Url is not specified");
				return 1;
			}
			var inputFileName = Path.GetFullPath(zipFileName);
			Console.WriteLine("Package file: {0}", inputFileName);
			Console.WriteLine("Target url: {0}", inboxUrl);
			var client = new HttpClient();
			using (var zipStream = new FileStream(inputFileName, FileMode.Open))
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

		static int Test(string[] args)
		{
			var asm = Assembly.LoadFrom(@"C:\Users\sergeysu\logjoint\trunk\tests\integration\bin\Debug\netcoreapp2.0\logjoint.integration.tests.dll");
			var runner = asm.CreateInstance("LogJoint.Tests.Integration.TestRunner");
			var task = (Task)runner.GetType().InvokeMember("RunPluginTests", BindingFlags.InvokeMethod, null, runner, new object[] { @"C:\Users\sergeysu\logjoint\trunk\extensions\chromium\plugin\bin\Debug\netstandard2.0" });
			task.Wait();
			return 0;
		}
	}
}
