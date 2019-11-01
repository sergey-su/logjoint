using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogJoint.Extensibility;

namespace LogJoint.Tests.Integration
{
	public class TestRunner
	{
		static async Task Main(string[] args)
		{
			string filter = null;
			Action<string> consumeValue = null;
			foreach (var arg in args)
			{
				if (consumeValue != null)
					consumeValue(arg);
				else if (arg == "--filter" || arg == "--f")
					consumeValue = val => filter = val;
			}
			await RunTests(Assembly.GetExecutingAssembly(), null, filter);
		}

		public async Task RunPluginTests(string pluginDir, string filters = null)
		{
			IPluginManifest manifest = new PluginManifest(pluginDir);
			var testFile = manifest.Test ?? throw new ArgumentException($"Plug-in does not contain tests: {manifest.AbsolulePath}");
			var testsAsm = Assembly.LoadFrom(testFile.AbsolulePath);
			await RunTests(testsAsm, manifest.PluginDirectory, filters);
		}

		static string FilterToRegexTemplate(string f) =>
			f.Split('*')
			.Aggregate(
				(i: 0, sb: new StringBuilder()),
				(agg, part) => (i: agg.i + 1, sb: agg.sb.Append(agg.i > 0 ? ".*?" : "").Append(Regex.Escape(part))),
				agg => agg.sb.ToString()
			);

		static async Task<bool> RunTests(
			Assembly testsAsm,
			string localPluginsList,
			string filters
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
			bool anyFailed = false;
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
					Console.Write($"{fullTestName} ... ");
					if (!string.IsNullOrEmpty(fixtureIgnore))
					{
						Console.Write($"Fixture ignored ({fixtureIgnore})");
						continue;
					}
					if (!string.IsNullOrEmpty(testIgnore))
					{
						Console.Write($"Ignored ({testIgnore})");
						continue;
					}
					var stage = "";
					try
					{
						stage = " at app startup";
						var app = await TestAppInstance.Create(new TestAppConfig
						{
							LocalPluginsList = localPluginsList
						});
						try
						{
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
							Console.WriteLine($"Passed");
						}
						finally
						{
							stage = " at app cleanup";
							await app.Dispose();
						}
					}
					catch (Exception e)
					{
						anyFailed = true;
						Console.WriteLine($"Failed{stage}");
						Console.WriteLine($"Exception of type {e.GetType().Name}: {e.Message}{Environment.NewLine}Stack: {e.StackTrace}");
						for (; ; )
						{
							Exception inner = e.InnerException;
							if (inner == null)
								break;
							Console.WriteLine($"--- inner: {inner.GetType().Name} '{inner.Message}'{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
							e = inner;
						}
					}
				}
			}
			return anyFailed;
		}
	}
}
