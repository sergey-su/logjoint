using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LogJoint.UI;

namespace LogJoint
{
	class PluginsManager: IDisposable
	{
		readonly List<PluginBase> plugins = new List<PluginBase>();
		readonly LJTraceSource tracer;
		readonly ILogJointApplication entryPoint;
		readonly TabControl menuTabControl;

		public PluginsManager(
			LJTraceSource tracer, ILogJointApplication entryPoint, TabControl menuTabControl)
		{
			this.tracer = tracer;
			this.entryPoint = entryPoint;
			this.menuTabControl = menuTabControl;
			
			InitPlugins();
			LoadTabExtensions();
		}

		private void InitPlugins()
		{
			using (tracer.NewFrame)
			{
				string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				tracer.Info("Plugins directory: {0}", thisPath);
				foreach (string pluginPath in Directory.GetFiles(thisPath, "*.plugin.dll"))
				{
					tracer.Info("---> Plugin found {0}", pluginPath);
					Assembly pluginAsm;
					try
					{
						pluginAsm = Assembly.LoadFrom(pluginPath);
					}
					catch (Exception e)
					{
						tracer.Error(e, "Filed to load plugin");
						continue;
					}
					Type pluginType = pluginAsm.GetType("LogJoint.Plugin");
					if (pluginType == null)
					{
						tracer.Warning("Plugin class not found in plugin assembly");
						continue;
					}
					if (!typeof(PluginBase).IsAssignableFrom(pluginType))
					{
						tracer.Warning("Plugin object doesn't support IPlugin");
						continue;
					}
					PluginBase plugin;
					try
					{
						plugin = (PluginBase)Activator.CreateInstance(pluginType);
					}
					catch (Exception e)
					{
						tracer.Error(e, "Filed to create an instance of plugin");
						continue;
					}
					try
					{
						plugin.Init(entryPoint);
					}
					catch (Exception e)
					{
						plugin.Dispose();
						tracer.Error(e, "Filed to init an instance of plugin");
						continue;
					}
					plugins.Add(plugin);
				}
			}
		}

		void LoadTabExtensions()
		{
			foreach (PluginBase plugin in plugins)
			{
				foreach (IMainFormTabExtension ext in plugin.MainFormTagExtensions)
				{
					TabPage tab = new TabPage(ext.Caption);
					menuTabControl.TabPages.Add(tab);
					tab.Controls.Add(ext.TapPage);
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
