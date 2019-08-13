using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LogJoint.Extensibility
{
	public class PluginsManager: IDisposable, IPluginsManager, IPluginsManagerInternal
	{
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IPluginFormatsManager pluginFormatsManager;
		readonly List<object> plugins = new List<object> ();
		readonly LJTraceSource tracer;
		readonly Dictionary<Type, object> types = new Dictionary<Type, object>();

		public PluginsManager(
			ITraceSourceFactory traceSourceFactory,
			Telemetry.ITelemetryCollector telemetry,
			IShutdown shutdown,
			IPluginFormatsManager pluginFormatsManager)
		{
			this.tracer = traceSourceFactory.CreateTraceSource("Extensibility", "plugins-mgr");
			this.telemetry = telemetry;
			this.pluginFormatsManager = pluginFormatsManager;

			shutdown.Cleanup += (s, e) => Dispose();
		}

		void IPluginsManagerInternal.LoadPlugins(object appEntryPoint)
		{
			InitPlugins(appEntryPoint);
			RegisterInteropClasses();
		}

		IPluginManifest IPluginsManagerInternal.LoadManifest(string pluginDirectory)
		{
			return new PluginManifest(pluginDirectory);
		}

		void IPluginsManager.Register<PluginType>(PluginType plugin)
		{
			types[typeof(PluginType)] = plugin;
		}

		PluginType IPluginsManager.Get<PluginType>()
		{
			if (!types.TryGetValue(typeof(PluginType), out var plugin))
				return null;
			return plugin as PluginType;
		}

		private void InitPlugins(object entryPoint)
		{
			using (tracer.NewFrame)
			{
				string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string pluginsDirectory = Path.Combine(thisPath, "Plugins");
				bool pluginsDirectoryExists = Directory.Exists(pluginsDirectory);
				tracer.Info("plugins directory: {0}{1}", pluginsDirectory, !pluginsDirectoryExists ? " (MISSING!)" : "");
				if (!pluginsDirectoryExists)
					return;

				var manifests = Directory.EnumerateDirectories(pluginsDirectory).Select(pluginDirectory =>
				{
					tracer.Info("---> plugin found {0}", pluginDirectory);
					IPluginManifest manifest = null;
					try
					{
						manifest = new PluginManifest(pluginDirectory);
						manifest.ValidateFilesExist();
					}
					catch (Exception e)
					{
						tracer.Error(e, "Bad manifest");
						telemetry.ReportException(e, "Bad manifest in " + pluginDirectory);
					}
					return manifest;
				}).Where(manifest => manifest != null).ToDictionary(manifest => manifest.Id);

				void LoadPlugin(IPluginManifest manifest)
				{
					var pluginPath = manifest.Entry.AbsolulePath;

					tracer.Info("Loading plugin {0} from '{1}'", manifest.Id, pluginPath);

					Stopwatch sw = Stopwatch.StartNew();
					pluginFormatsManager.RegisterPluginFormats(manifest);
					var formatsLoadTime = sw.Elapsed;

					sw.Restart();
					Assembly pluginAsm;
					try
					{
						pluginAsm = Assembly.LoadFrom(pluginPath);
					}
					catch (Exception e)
					{
						throw new Exception("Failed to load plugin asm", e);
					}
					var loadTime = sw.Elapsed;
					sw.Restart();
					Type pluginType;
					try
					{
						pluginType = pluginAsm.GetType("LogJoint.Plugin");
					}
					catch (Exception e)
					{
						throw new Exception("Failed to load plugin type", e);
					}
					var typeLoadTime = sw.Elapsed;
					if (pluginType == null)
					{
						throw new Exception("plugin class not found in plugin assembly");
					}
					var ctr = pluginType.GetConstructor(new[] { entryPoint.GetType() });
					if (ctr == null)
					{
						throw new Exception("plugin class does not implement ctr with LogJoint.IApplication argument");
					}
					sw.Restart();
					object plugin;
					try
					{
						plugin = ctr.Invoke(new[] { entryPoint });
					}
					catch (Exception e)
					{
						throw new Exception("failed to create an instance of plugin", e);
					}
					var instantiationTime = sw.Elapsed;

					tracer.Info("plugin {0} accepted. times: loading formats={1}, loading dll={2}, type loading={3}, instantiation={4}",
						Path.GetFileName(pluginPath), formatsLoadTime, loadTime, typeLoadTime, instantiationTime);
					plugins.Add(plugin);
				}

				var visitedPluginIds = new HashSet<string>();

				void LoadPluginAndDependencies(string id)
				{
					if (visitedPluginIds.Add(id))
					{
						if (!manifests.TryGetValue(id, out var manifest))
						{
							throw new Exception($"Required plugin '{id}' not found");
						}

						foreach (var dep in manifest.Dependencies)
							LoadPluginAndDependencies(dep.PluginId);

						var sdks = manifest.Dependencies.SelectMany(dep =>
						{
							if (!manifests.TryGetValue(dep.PluginId, out var depManifest))
								throw new Exception($"Plugin {manifest.Id} requires {dep.PluginId} that is not found");
							return depManifest.Files.Where(f => f.Type == PluginFileType.SDK);
						}).ToArray();
						Assembly dependencyResolveHandler(object s, ResolveEventArgs e)
						{
							var fileName = $"{(new AssemblyName(e.Name)).Name}.dll";
							var sdkFile = sdks.FirstOrDefault(f => Path.GetFileName(f.RelativePath) == fileName);
							try
							{
								return sdkFile != null ? Assembly.LoadFrom(sdkFile.AbsolulePath) : null;
							}
							catch (Exception ex)
							{
								throw new Exception($"Failed to load SDK asm '{sdkFile.AbsolulePath}' requested by {manifest.Id}", ex);
							}
						}
						AppDomain.CurrentDomain.AssemblyResolve += dependencyResolveHandler;
						try
						{
							LoadPlugin(manifest);
						}
						finally
						{
							AppDomain.CurrentDomain.AssemblyResolve -= dependencyResolveHandler;
						}
					}
				}

				foreach (string pluginId in manifests.Keys)
				{
					try
					{
						LoadPluginAndDependencies(pluginId);
					}
					catch (Exception e)
					{
						tracer.Error(e, $"Failed to load plugin '{pluginId}' or its dependencies");
						telemetry.ReportException(e, "Loading of plugin " + pluginId);
					}
				}
			}
		}

		void RegisterInteropClasses()
		{
			#if MONOMAC
			foreach (var plugin in plugins)
				ObjCRuntime.Runtime.RegisterAssembly (plugin.GetType().Assembly);
			#endif
		}

		public void Dispose()
		{
			foreach (var plugin in plugins)
			{
				if (plugin is IDisposable disposable)
					disposable.Dispose();
			}
			plugins.Clear();
		}
	}
}
