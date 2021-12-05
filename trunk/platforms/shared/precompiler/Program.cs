using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint
{
	class Program
	{
		static async Task Main(string[] args)
		{
			string pluginPath = null;
			string inputFormatFilePath = null;
			string outputFormatFilePath = null;
			if (args.ElementAtOrDefault(0) == "plugin")
			{
				pluginPath = Path.GetFullPath(args.ElementAtOrDefault(1));
			}
			if (args.ElementAtOrDefault(0) == "format")
			{
				inputFormatFilePath = args.ElementAtOrDefault(1);
				outputFormatFilePath = args.ElementAtOrDefault(2);
			}

			if (pluginPath == null && (inputFormatFilePath == null || outputFormatFilePath == null))
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("   logjoint.precompiler plugin <plugin dirertory>                   - precompile code for all format definitions in the plugin");
				Console.WriteLine("   logjoint.precompiler format <in format file> <out format file>   - precompile code the format definition in the given file");
				return;
			}

			var appDataDir = Path.Combine(Path.GetTempPath(),
				$"logjoint.precompiler.workdir.{DateTime.Now:yyyy'-'MM'-'dd'T'HH'-'mm'-'ss'.'fff}");

			Console.WriteLine("Precompiler data and logs path: {0}", appDataDir);

			var tempFormatsDir = Path.Combine(appDataDir, "TempFormats");
			Directory.CreateDirectory(tempFormatsDir);
			var tempFormatFilePath = Path.Combine(tempFormatsDir, "temp.format.xml");
			if (inputFormatFilePath != null)
				File.Copy(sourceFileName: inputFormatFilePath, destFileName: tempFormatFilePath);

			ISynchronizationContext serialSynchronizationContext = new SerialSynchronizationContext();
			var traceListener = new TraceListener(Path.Combine(appDataDir, "precompiler-debug.log") + ";logical-thread=1");

			ModelObjects modelObjects = await serialSynchronizationContext.Invoke(() =>
			{
				return ModelFactory.Create(
					new ModelConfig
					{
						WorkspacesUrl = "",
						TelemetryUrl = "",
						IssuesUrl = "",
						AutoUpdateUrl = "",
						WebContentCacheConfig = null,
						LogsDownloaderConfig = null,
						AppDataDirectory = appDataDir,
						TraceListeners = new TraceListener[] { traceListener },
						DisableLogjointInstancesCounting = true,
						AdditionalFormatDirectories = new string[] { tempFormatsDir },
						UserCodeAssemblyProvider = new ComplingUserCodeAssemblyProvider(new DefaultMetadataReferencesProvider()),
					},
					serialSynchronizationContext,
					(_1) => null,
					(_1, _2, _3) => null,
					null,
					RegularExpressions.FCLRegexFactory.Instance
				);
			});

			await serialSynchronizationContext.Invoke(() =>
			{
				if (pluginPath != null)
				{
					HandlePlugin(pluginPath, modelObjects);
				}

				if (inputFormatFilePath != null)
				{
					HandleFormatFile(tempFormatFilePath, inputFormatFilePath, modelObjects);
					File.Copy(sourceFileName: tempFormatFilePath, destFileName: outputFormatFilePath, overwrite: true);
				}
			});
		}

		private static void HandlePlugin(string pluginPath, ModelObjects modelObjects)
		{
			modelObjects.PluginsManager.LoadPlugins(new
			{
				Model = modelObjects.ExpensibilityEntryPoint,
				Presentation = (LogJoint.UI.Presenters.IPresentation)null
			}, pluginPath, preferTestPluginEntryPoints: true);

			Extensibility.IPluginManifest pluginManifest = modelObjects.PluginsManager.InstalledPlugins.FirstOrDefault();
			if (pluginManifest == null)
			{
				Console.WriteLine("ERROR: Failed to load plugin from '{0}'", pluginPath);
				return;
			}
			foreach (Extensibility.IPluginFile formatFile in pluginManifest.Files.Where(f => f.Type == Extensibility.PluginFileType.FormatDefinition))
			{
				HandleFormatFile(formatFile.AbsolutePath, formatFile.AbsolutePath, modelObjects);
			}
		}

		private static void HandleFormatFile(string formatFilePath, string formatFilePathForLogging, ModelObjects modelObjects)
		{
			IUserDefinedFactory formatFactory = modelObjects.UserDefinedFormatsManager.Items.FirstOrDefault(
				factory => factory.Location == formatFilePath);
			if (formatFactory == null)
			{
				Console.WriteLine("ERROR: Failed to load format definition '{0}'", formatFilePathForLogging);
				return;
			}
			var precomp = formatFactory as IPrecompilingLogProviderFactory;
			if (precomp == null)
			{
				Console.WriteLine("WARNING: Skipping '{0}' - precompilation is not supported for it", formatFilePathForLogging);
				return;
			}
			var formatDoc = XDocument.Load(formatFilePath);
			var fieldsConfigElement =
				formatDoc.Elements("format").Elements("regular-grammar").Elements("fields-config").FirstOrDefault();
			if (fieldsConfigElement == null)
			{
				Console.WriteLine("ERROR: failed tofind fields config in '{0}'", formatFilePathForLogging);
				return;
			}
			byte[] asmBytes = precomp.Precompile(LJTraceSource.EmptyTracer);
			XElement precompiledElement = fieldsConfigElement.Element("precompiled");
			if (precompiledElement == null)
			{
				precompiledElement = new XElement("precompiled");
				fieldsConfigElement.Add(precompiledElement);
			}
			precompiledElement.RemoveAll();
			precompiledElement.Add(new XCData(Convert.ToBase64String(asmBytes)));

			formatDoc.Save(formatFilePath);

			Console.WriteLine("Successfully precompiled '{0}'", formatFilePathForLogging);
		}
	}
}