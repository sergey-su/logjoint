using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace LogJoint.Tests.Integration
{
	public static class PluginUtils
	{
		public static string GetPluginDirectory(string pluginName)
		{
			return Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				"Plugins",
				pluginName
			);
		}

		public static void LoadPluginAssemblies(IPluginManifest pluginManifest, Action touchTypes)
		{
			var dlls = pluginManifest.Files.Where(f => f.Type == PluginFileType.Library).ToDictionary(f => Path.GetFileName(f.RelativePath));
			Assembly handler(object s, ResolveEventArgs e)
			{
				if (dlls.TryGetValue($"{new AssemblyName(e.Name).Name}.dll", out var pluginFile))
					return Assembly.LoadFrom(pluginFile.AbsolulePath);
				return null;
			}
			AppDomain.CurrentDomain.AssemblyResolve += handler;
			try
			{
				touchTypes();
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= handler;
			}
		}
	};
}
