using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LogJoint.UI;
using System.Diagnostics;

namespace LogJoint
{
	class PluginsManager: IDisposable
	{
		readonly List<PluginBase> plugins = new List<PluginBase>();
		readonly LJTraceSource tracer;
		readonly ILogJointApplication entryPoint;
		readonly TabControl menuTabControl;

		public PluginsManager(
			LJTraceSource tracer, 
			ILogJointApplication entryPoint,
			TabControl menuTabControl,
			UI.Presenters.MainForm.IPresenter mainFormPresenter,
			Telemetry.ITelemetryCollector telemetry)
		{
			this.tracer = tracer;
			this.entryPoint = entryPoint;
			this.menuTabControl = menuTabControl;
			
			InitPlugins();
			LoadTabExtensions();

			menuTabControl.Selected += (s, e) =>
			{
				var t = menuTabControl.SelectedTab;
				if (t == null)
					return;
				var ext = t.Tag as IMainFormTabExtension;
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
			mainFormPresenter.Closing += (s, e) => Dispose();
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
					TabPage tab = new TabPage(ext.Caption) { Tag = ext };
					menuTabControl.TabPages.Add(tab);
					tab.Controls.Add(ext.PageControl);
				}
			}
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
