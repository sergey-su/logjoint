using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using LogJoint.Postprocessing;

namespace LogJoint.Extensibility
{
	public class PluginsManager: IDisposable, IPluginsManager, IPluginsManagerInternal
	{
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly IPluginFormatsManager pluginFormatsManager;
		readonly IChangeNotification changeNotification;
		readonly List<object> plugins = new List<object> ();
		ImmutableArray<IPluginManifest> pluginManifests = ImmutableArray.Create<IPluginManifest>();
		readonly LJTraceSource tracer;
		readonly AutoUpdate.IUpdateDownloader updateDownloader;
		readonly Dictionary<Type, object> types = new Dictionary<Type, object>();
		readonly AutoUpdate.IUpdateDownloader pluginsIndexDownloader;
		readonly IPluginsIndexFactory pluginsIndexFactory;
		IPluginsIndex pluginsIndex = null;
		ImmutableDictionary<string, bool> installationRequests = ImmutableDictionary.Create<string, bool>();

		public PluginsManager(
			ITraceSourceFactory traceSourceFactory,
			Telemetry.ITelemetryCollector telemetry,
			IShutdown shutdown,
			IPluginFormatsManager pluginFormatsManager,
			AutoUpdate.IUpdateDownloader pluginsIndexDownloader,
			IPluginsIndexFactory pluginsIndexFactory,
			IChangeNotification changeNotification,
			AutoUpdate.IUpdateDownloader updateDownloader
		)
		{
			this.tracer = traceSourceFactory.CreateTraceSource("Extensibility", "plug-ins-mgr");
			this.telemetry = telemetry;
			this.pluginFormatsManager = pluginFormatsManager;
			this.pluginsIndexDownloader = pluginsIndexDownloader;
			this.pluginsIndexFactory = pluginsIndexFactory;
			this.changeNotification = changeNotification;
			this.updateDownloader = updateDownloader;

			shutdown.Cleanup += (s, e) => Dispose();
		}

		void IPluginsManagerInternal.LoadPlugins(object appEntryPoint, string localPluginsList)
		{
			InitPlugins(appEntryPoint, localPluginsList);
		}

		bool IPluginsManagerInternal.IsConfigured => pluginsIndexDownloader.IsDownloaderConfigured && updateDownloader.IsDownloaderConfigured;

		IEnumerable<Assembly> IPluginsManagerInternal.PluginAssemblies => plugins.Select(plugin => plugin.GetType().Assembly);

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

		IReadOnlyList<IPluginManifest> IPluginsManagerInternal.InstalledPlugins => pluginManifests;

		async Task<IReadOnlyList<IPluginInfo>> IPluginsManagerInternal.FetchAllPlugins(CancellationToken cancellation)
		{
			if (!pluginsIndexDownloader.IsDownloaderConfigured)
			{
				return ImmutableArray.Create<IPluginInfo>();
			}
			using (var resultStream = new MemoryStream())
			{
				var result = await pluginsIndexDownloader.DownloadUpdate(pluginsIndex?.ETag, resultStream, cancellation);
				if (result.Status == AutoUpdate.DownloadUpdateResult.StatusCode.Failure)
				{
					this.tracer.Error("Failed to download plug-ins index: {0}", result.ErrorMessage);
					throw new Exception("Failed to fetch");
				}
				else if (result.Status == AutoUpdate.DownloadUpdateResult.StatusCode.Success)
				{
					resultStream.Position = 0;
					pluginsIndex = pluginsIndexFactory.Create(resultStream, result.ETag);
				}
				return MakePluginInfoList(pluginsIndex, pluginManifests, telemetry);
			}
		}

		IReadOnlyDictionary<string, bool> IPluginsManagerInternal.InstallationRequests => installationRequests;

		IPluginInstallationRequestsBuilder IPluginsManagerInternal.CreatePluginInstallationRequestsBuilder()
		{
			return new PluginInstallationRequestsBuilder(this);
		}

		private void InitPlugins(object appEntryPoint, string localPluginsList)
		{
			using (tracer.NewFrame)
			{
				string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string pluginsDirectory = Path.Combine(thisPath, "Plugins");
				bool pluginsDirectoryExists = Directory.Exists(pluginsDirectory);
				tracer.Info("plugins directory: {0}{1}", pluginsDirectory, !pluginsDirectoryExists ? " (MISSING!)" : "");
				var localPluginDirs =
					localPluginsList.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(dir => Path.IsPathRooted(dir) ? dir : Path.GetFullPath(Path.Combine(thisPath, dir)))
					.Select(dir => (dir, exists: Directory.Exists(dir)));
				tracer.Info("local plugin directories: {0}", string.Join(",",
					localPluginDirs.Select(d => $"{d.dir}{(d.exists ? "" : " (MISSING!)")}"))
				);

				var manifests =
					(pluginsDirectoryExists ? Directory.EnumerateDirectories(pluginsDirectory) : new string[0])
					.Union(localPluginDirs.Where(d => d.exists).Select(d => d.dir))
					.Select(pluginDirectory =>
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
				}).Where(manifest => manifest != null).ToDictionarySafe(
					manifest => manifest.Id, manifest => manifest,
					(exisingManifest, newManifest) => newManifest
				);

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

					var modelEntryPoint = appEntryPoint.GetType().InvokeMember("Model", BindingFlags.GetProperty, null, appEntryPoint, new object[0]);
					if (modelEntryPoint == null)
					{
						throw new Exception("Model is missing from app entry point");
					}
					var presentationEntryPoint = appEntryPoint.GetType().InvokeMember("Presentation", BindingFlags.GetProperty, null, appEntryPoint, new object[0]);
					if (presentationEntryPoint == null)
					{
						throw new Exception("Presentation is missing from app entry point");
					}

					TimeSpan instantiationTime = TimeSpan.Zero;
					object plugin = null;

					bool TryCtr(params object[] @params)
					{
						var ctr = pluginType.GetConstructor(@params.Select(p => p.GetType()).ToArray());
						if (ctr == null)
						{
							return false;
						}
						sw.Restart();
						try
						{
							plugin = ctr.Invoke(@params);
						}
						catch (Exception e)
						{
							throw new Exception("failed to create an instance of plugin", e);
						}
						instantiationTime = sw.Elapsed;
						return true;
					}

					if (!TryCtr(appEntryPoint)
					 && !TryCtr(modelEntryPoint)
					 && !TryCtr(modelEntryPoint, presentationEntryPoint))
					{
						throw new Exception("plugin class does not implement ctr with LogJoint.IApplication argument, or with LogJoint.IModel argument, or with IModel and IPresentation arguments");
					}


					tracer.Info("plugin {0} accepted. times: loading formats={1}, loading dll={2}, type loading={3}, instantiation={4}",
						Path.GetFileName(pluginPath), formatsLoadTime, loadTime, typeLoadTime, instantiationTime);
					plugins.Add(plugin);
					pluginManifests = pluginManifests.Add(manifest);
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
							LoadPluginAndDependencies(dep);

						var sdks = manifest.Dependencies.SelectMany(dep =>
						{
							if (!manifests.TryGetValue(dep, out var depManifest))
								throw new Exception($"Plugin {manifest.Id} requires {dep} that is not found");
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

		public void Dispose()
		{
			foreach (var plugin in plugins)
			{
				if (plugin is IDisposable disposable)
					disposable.Dispose();
			}
			plugins.Clear();
			pluginManifests = pluginManifests.Clear();
		}

		static IReadOnlyList<IPluginInfo> MakePluginInfoList(
			IPluginsIndex index,
			ImmutableArray<IPluginManifest> installedPluginsManifests,
			Telemetry.ITelemetryCollector telemetryCollector
		)
		{
			Dictionary<string, PluginInfo> map = new Dictionary<string, PluginInfo>();

			var installedPluginsMap = installedPluginsManifests.ToLookup(p => p.Id);

			foreach (var indexedPlugin in index.Plugins)
			{
				map[indexedPlugin.Id] = new PluginInfo
				{
					id = indexedPlugin.Id,
					version = indexedPlugin.Version,
					name = indexedPlugin.Name,
					description = indexedPlugin.Description,
					indexItem = indexedPlugin,
					installedPluginManifest = installedPluginsMap[indexedPlugin.Id].FirstOrDefault(),
					dependenciesIds = indexedPlugin.Dependencies,
				};
			}

			installedPluginsManifests
				.Where(installedPlugin => !map.ContainsKey(installedPlugin.Id))
				.Select(installedPlugin => new PluginInfo
				{
					id = installedPlugin.Id,
					version = installedPlugin.Version,
					name = installedPlugin.Name,
					description = installedPlugin.Description,
					indexItem = null,
					installedPluginManifest = installedPlugin,
					dependenciesIds = installedPlugin.Dependencies
				})
				.ToList()
				.ForEach(p => map[p.id] = p);

			foreach (var p in map.Values.ToArray())
			{
				var deps = p.dependenciesIds.Select(depId =>
				{
					bool resolved = map.TryGetValue(depId, out var dep);
					return (resolved, depId, dep);
				}).ToArray();
				var unresolvedDeps = deps.Where(d => !d.resolved).Select(d => d.depId).ToArray();
				if (unresolvedDeps.Length > 0)
				{
					telemetryCollector.ReportException(new Exception("Bad plug-ins index"),
						$"Plug-in {p.id} depends on non-indexed plug-in(s) {string.Join(",", unresolvedDeps)}");
				}
				var resolvedDeps = deps.Where(d => d.resolved).Select(d => d.dep).ToList();
				p.dependencies = resolvedDeps.AsReadOnly();
				resolvedDeps.ForEach(d => d.dependants = d.dependants.Add(p));
			}

			return ImmutableArray.CreateRange(map.Values);
		}

		class PluginInfo : IPluginInfo
		{
			public string id, name, description;
			public IPluginIndexItem indexItem;
			public Version version;
			public IPluginManifest installedPluginManifest;
			public IReadOnlyList<string> dependenciesIds;
			public IReadOnlyList<IPluginInfo> dependencies;
			public ImmutableList<IPluginInfo> dependants = ImmutableList.Create<IPluginInfo>();

			string IPluginInfo.Id => id;
			Version IPluginInfo.Version => version;
			string IPluginInfo.Name => name;
			string IPluginInfo.Description => description;
			IPluginIndexItem IPluginInfo.IndexItem => indexItem;
			IReadOnlyList<IPluginInfo> IPluginInfo.Dependencies => dependencies;
			IReadOnlyList<IPluginInfo> IPluginInfo.Dependants => dependants;
			IPluginManifest IPluginInfo.InstalledPluginManifest => installedPluginManifest;
		};

		class PluginInstallationRequestsBuilder : IPluginInstallationRequestsBuilder
		{
			readonly PluginsManager owner;
			ImmutableDictionary<string, bool> installationRequests;

			public PluginInstallationRequestsBuilder(PluginsManager owner)
			{
				this.owner = owner;
				this.installationRequests = owner.installationRequests;
			}

			IReadOnlyDictionary<string, bool> IPluginInstallationRequestsBuilder.InstallationRequests => installationRequests;

			void IPluginInstallationRequestsBuilder.RequestInstallationState(IPluginInfo plugin, bool desiredState)
			{
				void SetInstallationState(IPluginInfo p, bool state)
				{
					if (installationRequests.TryGetValue(p.Id, out var currentRequest))
					{
						if (currentRequest != state)
						{
							installationRequests = installationRequests.Remove(p.Id);
							owner.changeNotification.Post();
						}
					}
					else if ((p.InstalledPluginManifest != null) != state)
					{
						installationRequests = installationRequests.SetItem(p.Id, state);
						owner.changeNotification.Post();
					}
				}

				void TraverseDependencies(IPluginInfo p, bool traverseForward, HashSet<IPluginInfo> result)
				{
					if (result.Add(p))
					{
						foreach (var dep in (traverseForward ? p.Dependencies : p.Dependants))
							TraverseDependencies(dep, traverseForward, result);
					}
				}

				var affectedPlugins = new HashSet<IPluginInfo>();
				TraverseDependencies(plugin, desiredState, affectedPlugins);

				foreach (var affectedPlugin in affectedPlugins)
				{
					SetInstallationState(affectedPlugin, desiredState);
				}
			}

			void IPluginInstallationRequestsBuilder.ApplyRequests()
			{
				owner.installationRequests = installationRequests;
				owner.changeNotification.Post();
			}
		};
	}
}
