using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using LogJoint.Extensibility;
using NSubstitute;

namespace LogJoint.Tests.Integration
{
	public class TestRunner
	{
		static async Task Main(string[] args)
		{
			string filter = null;
			foreach (var arg in args)
			{
				var split = arg.Split('=');
				if (split.ElementAtOrDefault(0) == "--filter")
					filter = split.ElementAtOrDefault(1);
			}
			await RunTests(Assembly.GetExecutingAssembly(), null, filter, null);
		}

		public async Task DownloadPluginDependencies(IPluginManifest manifest, List<string> result)
		{
			using (var http = new HttpClient())
			{
				IPluginsIndexFactory indexFactory = new PluginsIndex.Factory(Substitute.For<Telemetry.ITelemetryCollector>());
				IPluginsIndex index;
				using (var indexResponseStream = await http.GetStreamAsync(Properties.Settings.Default.PluginsUrl))
					index = indexFactory.Create(indexResponseStream, "dummy");
				var pluginsLookup = index.Plugins.ToDictionary(p => p.Id);
				foreach (var depId in manifest.Dependencies) // no support for nested deps for now
				{
					if (!pluginsLookup.TryGetValue(depId, out var dep))
						throw new Exception($"Can not resolve dependency {dep}");
					var tempFilePath = Path.GetTempFileName();
					using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create))
					using (var networkStream = await http.GetStreamAsync(dep.Location))
						await networkStream.CopyToAsync(tempFileStream);
					var tempDir = Path.Combine(Path.GetTempPath(),
						$"logjoint.int.tests.bin.{Guid.NewGuid().ToString("N")}");
					ZipFile.ExtractToDirectory(tempFilePath, tempDir);
					File.Delete(tempFilePath);
					result.Add(tempDir);
				}
			}
		}

		public async Task RunPluginTests(string pluginDir, string filters, bool needsDepsDownload)
		{
			IPluginManifest manifest = new PluginManifest(pluginDir);
			var testFile = manifest.Test ?? throw new ArgumentException($"Plug-in does not contain tests: {manifest.AbsolulePath}");
			var testsAsm = Assembly.LoadFrom(testFile.AbsolulePath);
			var downloadedDependenciesDirs = new List<string>();
			try
			{
				if (manifest.Dependencies.Count > 0 && needsDepsDownload)
					await DownloadPluginDependencies(manifest, downloadedDependenciesDirs);
				await RunTests(
					testsAsm,
					string.Join(';', downloadedDependenciesDirs.Union(new[] { manifest.PluginDirectory })),
					filters,
					app =>
					{
						var autoAccept = app.AutoAcceptPreprocessingUserInteration();
						return () =>
						{
							autoAccept.Dispose();
						};
					}
				);
			}
			finally
			{
				downloadedDependenciesDirs.ForEach(d => Directory.Delete(d, true));
			}
		}

		static string FilterToRegexTemplate(string f) =>
			f.Split('*')
			.Aggregate(
				(i: 0, sb: new StringBuilder()),
				(agg, part) => (i: agg.i + 1, sb: agg.sb.Append(agg.i > 0 ? ".*?" : "").Append(Regex.Escape(part))),
				agg => agg.sb.ToString()
			);

		static void Log(string message)
		{
			System.Diagnostics.Debug.Print(message + Environment.NewLine);
			Console.WriteLine(message);
		}

		static async Task<bool> RunTests(
			Assembly testsAsm,
			string localPluginsList,
			string filters,
			Func<TestAppInstance, Action> prepareApp
		)
		{
			var fixtures = 
				testsAsm
				.GetTypes()
				.Select(t => (t, attr: t.GetCustomAttributes<IntegrationTestFixtureAttribute>().FirstOrDefault()))
				.Where(finfo => finfo.attr != null)
				.Select(finfo =>
				{
					var (t, attr) = finfo;
					var allMembers = t.GetMembers(BindingFlags.Public | BindingFlags.Instance);
					return (
						type: t,
						displayName: attr.Description ?? t.Name,
						ignore: attr.Ignore,
						tests:
							allMembers
							.Select(m => (m, attr: m.GetCustomAttributes<IntegrationTestAttribute>().FirstOrDefault()))
							.Where(tinfo => tinfo.attr != null)
							.Select(tinfo => (tinfo.m, displayName: tinfo.attr.Description ?? tinfo.m.Name, ignore: tinfo.attr.Ignore))
							.ToArray(),
						beforeEach:
							allMembers
							.FirstOrDefault(m => m.GetCustomAttributes(typeof(BeforeEachAttribute)).Any()),
						afterEach:
							allMembers
							.FirstOrDefault(m => m.GetCustomAttributes(typeof(AfterEachAttribute)).Any())
					);
				})
				.ToArray();
			var filtersRegex = filters != null ? new Regex(FilterToRegexTemplate(filters)) : new Regex(".");
			Task toAwaitable(object methodResult) => methodResult is Task task ? task : Task.FromResult(0);
			int testsCount = 0;
			int skippedCount = 0;
			int failedCount = 0;
			int passedCount = 0;
			foreach (var (type, fixtureDisplayName, fixtureIgnore, tests, beforeEach, afterEach) in fixtures)
			{
				if (tests.Length == 0)
					continue;
				var fixtureInstance = Activator.CreateInstance(type);
				foreach (var (testMethod, testDisplayName, testIgnore) in tests)
				{
					var fullTestName = $"{fixtureDisplayName} {testDisplayName}";
					if (!filtersRegex.IsMatch(fullTestName))
						continue;
					++testsCount;
					Log($"{fullTestName} ... ");
					if (!string.IsNullOrEmpty(fixtureIgnore))
					{
						Log($"    Fixture ignored ({fixtureIgnore})");
						++skippedCount;
						continue;
					}
					if (!string.IsNullOrEmpty(testIgnore))
					{
						Log($"    Ignored ({testIgnore})");
						++skippedCount;
						continue;
					}
					var stage = "";
					string appDataDirectory = null;
					try
					{
						stage = " at app startup";
						var app = await TestAppInstance.Create(new TestAppConfig
						{
							LocalPluginsList = localPluginsList
						});
						Action finalizeApp = null;
						try
						{
							finalizeApp = prepareApp?.Invoke(app);
							appDataDirectory = app.AppDataDirectory;
							await app.SynchronizationContext.InvokeAndAwait(async () =>
							{
								var methodFlags = BindingFlags.InvokeMethod;
								stage = " at stage BeforeEach";
								if (beforeEach != null)
									await toAwaitable(type.InvokeMember(beforeEach.Name, methodFlags, null, fixtureInstance, new[] { app }));
								stage = "";
								await toAwaitable(type.InvokeMember(testMethod.Name, methodFlags, null, fixtureInstance, new[] { app }));
								stage = " at stage AfterEach";
								if (afterEach != null)
									await toAwaitable(type.InvokeMember(afterEach.Name, methodFlags, null, fixtureInstance, new[] { app }));
							});
							Log($"    Passed");
							++passedCount;
						}
						finally
						{
							finalizeApp?.Invoke();
							await app.Dispose();
						}
					}
					catch (Exception e)
					{
						failedCount++;
						Log($"    Failed{stage}");
						if (appDataDirectory != null)
						{
							Log($"Logs and app data can be found in:'");
							Log($"    {appDataDirectory}");
						}
						Log($"Exception of type {e.GetType().Name} was thrown: {e.Message}{Environment.NewLine}Stack: {e.StackTrace}");
						for (; ; )
						{
							Exception inner = e.InnerException;
							if (inner == null)
								break;
							Log($"--- inner: {inner.GetType().Name} '{inner.Message}'{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
							e = inner;
						}
					}
				}
			}
			Log($"Summary: {testsCount} test(s) matched test filters, passed {passedCount}, skipped {skippedCount}, failed {failedCount}");
			return failedCount != 0;
		}
	}
}
