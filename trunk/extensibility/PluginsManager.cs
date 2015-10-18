using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using LogJoint.UI;
using System.Diagnostics;

namespace LogJoint
{
	class PluginsManager: IDisposable
	{
		readonly List<PluginBase> plugins = new List<PluginBase>();
		readonly LJTraceSource tracer;
		readonly ILogJointApplication entryPoint;
		readonly UI.Presenters.MainForm.IPresenter mainFormPresenter;

		public PluginsManager(
			ILogJointApplication entryPoint,
			UI.Presenters.MainForm.IPresenter mainFormPresenter,
			Telemetry.ITelemetryCollector telemetry,
			IShutdown shutdown)
		{
			this.tracer = new LJTraceSource("Extensibility", "plugins-mgr");
			this.entryPoint = entryPoint;
			this.mainFormPresenter = mainFormPresenter;

			InitPlugins();
			RegisterInteropClasses();
			LoadTabExtensions();

			mainFormPresenter.TabChanging += (sender, e) => 
			{
				var ext = e.CustomTabTag as IMainFormTabExtension;
				if (ext == null)
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

		private void InitPlugins()
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
						continue;
					}
					var loadTime = sw.Elapsed;
					Type pluginType = pluginAsm.GetType("LogJoint.Plugin");
					if (pluginType == null)
					{
						tracer.Warning("plugin class not found in plugin assembly");
						continue;
					}
					if (!typeof(PluginBase).IsAssignableFrom(pluginType))
					{
						tracer.Warning("plugin class doesn't inherit PluginBase");
						continue;
					}
					sw.Restart();
					PluginBase plugin;
					try
					{
						plugin = (PluginBase)Activator.CreateInstance(pluginType);
					}
					catch (Exception e)
					{
						tracer.Error(e, "failed to create an instance of plugin");
						continue;
					}
					var instantiationTime = sw.Elapsed;
					sw.Restart();
					try
					{
						plugin.Init(entryPoint);
					}
					catch (Exception e)
					{
						plugin.Dispose();
						tracer.Error(e, "failed to init an instance of plugin");
						continue;
					}
					var initTime = sw.Elapsed;
					tracer.Info("plugin {0} accepted. times: loading={1}, instantiation={2}, initialization={3}", 
						Path.GetFileName(pluginPath), loadTime, instantiationTime, initTime);
					plugins.Add(plugin);
				}
			}
		}

		void LoadTabExtensions()
		{
			foreach (PluginBase plugin in plugins)
			{
				foreach (IMainFormTabExtension ext in plugin.MainFormTabExtensions)
				{
					mainFormPresenter.AddCustomTab(
						ext.PageControl,
						ext.Caption,
						ext
					);
					//TabPage tab = new TabPage(ext.Caption) { Tag = ext };
					//menuTabControl.TabPages.Add(tab);
					//tab.Controls.Add(ext.PageControl);
				}
			}
		}

		void RegisterInteropClasses()
		{
			#if MONOMAC
			foreach (PluginBase plugin in plugins)
				MonoMac.ObjCRuntime.Runtime.RegisterAssembly (plugin.GetType().Assembly);
			#endif
		}

		public void Dispose()
		{
			foreach (PluginBase plugin in plugins)
			{
				plugin.Dispose();
			}
			plugins.Clear();
		}
	}
}
