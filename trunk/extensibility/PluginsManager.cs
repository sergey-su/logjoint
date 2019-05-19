using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LogJoint.Extensibility
{
	class PluginsManager: IDisposable
	{
		readonly List<object> plugins = new List<object> ();
		readonly LJTraceSource tracer;
		readonly LogJoint.IApplication entryPoint;
		readonly UI.Presenters.MainForm.IPresenter mainFormPresenter;

		public PluginsManager(
			LogJoint.IApplication entryPoint,
			UI.Presenters.MainForm.IPresenter mainFormPresenter,
			Telemetry.ITelemetryCollector telemetry,
			IShutdown shutdown)
		{
			this.tracer = new LJTraceSource("Extensibility", "plugins-mgr");
			this.entryPoint = entryPoint;
			this.mainFormPresenter = mainFormPresenter;

			InitPlugins(telemetry);
			RegisterInteropClasses();

			mainFormPresenter.TabChanging += (sender, e) => 
			{
				if (!(e.CustomTabTag is IMainFormTabExtension ext))
					return;
				try
				{
					ext.OnTabPageSelected();
				}
				catch (Exception ex)
				{
					telemetry.ReportException(ex, "activation of plugin tab: " + ext.Caption);
					tracer.Error(ex, "Failed to activate extension tab");
				}
			};

			shutdown.Cleanup += (s, e) => Dispose();
		}

		private void InitPlugins(Telemetry.ITelemetryCollector telemetry)
		{
			using (tracer.NewFrame)
			{
				string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				tracer.Info("plugins directory: {0}", thisPath);
				foreach (string pluginPath in Directory.GetFiles(thisPath, "*.plugin.dll"))
				{
					tracer.Info("---> plugin found {0}", pluginPath);
					Stopwatch sw = Stopwatch.StartNew();
					Assembly pluginAsm;
					try
					{
						pluginAsm = Assembly.LoadFrom(pluginPath);
					}
					catch (Exception e)
					{
						tracer.Error(e, "failed to load plugin");
						telemetry.ReportException(e, "loading pluging " + pluginPath);
						continue;
					}
					var loadTime = sw.Elapsed;
					sw.Restart ();
					Type pluginType;
					try
					{
						pluginType = pluginAsm.GetType ("LogJoint.Plugin");
					}
					catch (Exception e)
					{
						tracer.Error (e, "failed to load plugin type");
						telemetry.ReportException (e, "loading pluging " + pluginPath);
						continue;
					}
					var typeLoadTime = sw.Elapsed;
					if (pluginType == null)
					{
						tracer.Warning("plugin class not found in plugin assembly");
						continue;
					}
					var ctr = pluginType.GetConstructor (new [] { typeof (LogJoint.IApplication) });
					if (ctr == null)
					{
						tracer.Warning ("plugin class does not implement ctr with LogJoint.IApplication argument");
						continue;
					}
					sw.Restart();
					object plugin;
					try
					{
						plugin = ctr.Invoke (new [] { entryPoint });
					}
					catch (Exception e)
					{
						tracer.Error(e, "failed to create an instance of plugin");
						telemetry.ReportException(e, "creation of plugin " + pluginPath);
						continue;
					}
					var instantiationTime = sw.Elapsed;
					tracer.Info("plugin {0} accepted. times: loading dll={1}, type loading={2}, instantiation={3}", 
						Path.GetFileName(pluginPath), loadTime, typeLoadTime, instantiationTime);
					plugins.Add(plugin);
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
