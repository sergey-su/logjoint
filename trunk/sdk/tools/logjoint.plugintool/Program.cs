using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.PluginTool
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("logjoint.updatetool.exe <command>");
                Console.WriteLine("commands:");
                Console.WriteLine("  pack <path to manifest.xml> <zip-name> [prod]       Collects plug-in files referenced in the manifest and zips them into a deployable package.");
                Console.WriteLine("                                                      Once package is deployed, if prod flag is not specified, the package will be verified, but not published to users.");
                Console.WriteLine("  deploy <zip-name> <inbox url>                       Sends the plug-in package into the plug-ins inbox.");
                Console.WriteLine("  test <plugin> <host> [--filter=<value>]             Runs plug-in's integration tests with specified host app installation.");
                Console.WriteLine("                                                         <plugin> - local folder, or zip archive with packed plugin");
                Console.WriteLine("                                                         <host> - local folder, or url of zip archive with logjoint binaries");
                return 0;
            }

            switch (args[0])
            {
                case "pack":
                    return Pack(args.Skip(1).ToArray());
                case "deploy":
                    return Deploy(args.Skip(1).ToArray());
                case "test":
                    await Test(args.Skip(1).ToArray());
                    return 0;
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

        static void DeleteTemporarySafe(string path)
        {
            for (int attempt = 0; ; ++attempt)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    else if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to delete temporary {0} at attempt {1}: {2}", path, attempt + 1, e.Message);
                    if (attempt < 5)
                        System.Threading.Thread.Sleep(1000);
                    else
                        break;
                }
            }
        }

        static async Task<ArgumentDirectory> LocationArgumentToDirectory(string location, string locationName)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException($"{locationName} location is not specified");
            }
            ArgumentDirectory UnzipToTempFolder(string zipFileName)
            {
                var tempDir = Path.Combine(Path.GetTempPath(), $"logjoint.int.tests.bin.{Guid.NewGuid().ToString("N")}");
                ZipFile.ExtractToDirectory(zipFileName, tempDir);
                return new ArgumentDirectory { Path = tempDir, IsTemporary = true };
            }
            if (Directory.Exists(location))
            {
                return new ArgumentDirectory { Path = location };
            }
            else if (File.Exists(location))
            {
                return UnzipToTempFolder(location);
            }
            else if (Uri.TryCreate(location, UriKind.Absolute, out var hostUri))
            {
                if (hostUri.Scheme != "https")
                    throw new ArgumentException($"Only https is allowed in {locationName} location url. Given: '{location}'");
                var tempZipFileName = Path.GetTempFileName();
                try
                {
                    Console.Write("Downloading {0} ... ", location);
                    using (var cli = new HttpClient())
                    using (var zipFs = new FileStream(tempZipFileName, FileMode.Create))
                    {
                        var responseStream = await cli.GetStreamAsync(hostUri);
                        await responseStream.CopyToAsync(zipFs);
                    }
                    Console.WriteLine("Done");
                    return UnzipToTempFolder(tempZipFileName);
                }
                finally
                {
                    DeleteTemporarySafe(tempZipFileName);
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported {locationName} location: '{location}'");
            }
        }

        class ArgumentDirectory : IDisposable
        {
            public string Path;
            public bool IsTemporary;

            public void Dispose()
            {
                if (IsTemporary)
                    DeleteTemporarySafe(Path);
            }
        };

        static string FindHostIntegrationTestsAsm(string startDirectory)
        {
            var testsAsmName = "logjoint.integration.tests.dll";
            if (File.Exists(Path.Combine(startDirectory, testsAsmName)))
                return Path.Combine(startDirectory, testsAsmName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (Directory.Exists(Path.Combine(startDirectory, "Contents")))
                    return FindHostIntegrationTestsAsm(Path.Combine(startDirectory, "Contents", "MonoBundle"));
                if (Directory.Exists(Path.Combine(startDirectory, "MonoBundle")))
                    return FindHostIntegrationTestsAsm(Path.Combine(startDirectory, "MonoBundle"));
            }
            throw new ArgumentException($"Can not find required {testsAsmName} in location: {startDirectory}");
        }

        static async Task Test(string[] args)
        {
            var pluginLocation = args.ElementAtOrDefault(0);
            var hostLocation = args.ElementAtOrDefault(1);
            string filter = null;
            foreach (var arg in args.Skip(2))
            {
                var split = arg.Split('=');
                if (split.ElementAtOrDefault(0) == "--filter")
                    filter = split.ElementAtOrDefault(1);
            }

            using (var pluginDirectory = await LocationArgumentToDirectory(pluginLocation, "plugin"))
            using (var hostDirectory = await LocationArgumentToDirectory(hostLocation, "host"))
            {
                var hostTestsAssembly = Assembly.LoadFrom(FindHostIntegrationTestsAsm(hostDirectory.Path));
                var runner = hostTestsAssembly.CreateInstance("LogJoint.Tests.Integration.TestRunner");
                var runnerTask = (Task)runner.GetType().InvokeMember("RunPluginTests", BindingFlags.InvokeMethod, null, runner, new object[]
                {
                    pluginDirectory.Path,
                    filter,
                    hostDirectory.IsTemporary
                });
                await runnerTask;
            }
        }
    }
}
