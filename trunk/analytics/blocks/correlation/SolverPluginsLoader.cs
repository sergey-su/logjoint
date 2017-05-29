using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.Analytics.Correlation
{
	class SolverPluginsLoader
	{
		public SolverPluginsLoader()
		{
			StringBuilder initializationLog = new StringBuilder();
			try
			{
				Init(initializationLog);
			}
			catch (Exception e)
			{
				throw new Exception("Failed to initialize solver plugins: " + e.Message + "; log: " + initializationLog.ToString(), e);
			}
		}

		public void Init(StringBuilder loadingLog)
		{
			var pluginsDirectory = Path.Combine(
				Path.GetTempPath(), "LogJoint.Analytics", "SolverPlugins", IntPtr.Size == 4 ? "32" : "64");
			loadingLog.AppendFormat("Plugins directory: {0};", pluginsDirectory);
			Directory.CreateDirectory(pluginsDirectory);
			var msilPlugins = Assembly.GetExecutingAssembly().GetManifestResourceNames()
				.Where(resName => Regex.IsMatch(resName, @"^.+?solver\.\w+plugin.dll$", RegexOptions.IgnoreCase))
				.Select(resName => new { ResName = resName, IsMSIL = true })
				.ToList();
			loadingLog.AppendFormat("MSIL plugins: {0};", string.Join(",", msilPlugins.Select(p => p.ResName)));
			var nativePluginsResFolderName = IntPtr.Size == 4 ? "plugins32" : "plugins64";
			var nativePlugins = Assembly.GetExecutingAssembly().GetManifestResourceNames()
				.Where(resName => resName.ToLower().Contains(nativePluginsResFolderName))
				.Select(resName => new { ResName = resName, IsMSIL = false })
				.ToList();
			loadingLog.AppendFormat("Native plugins: {0};", string.Join(",", nativePlugins.Select(p => p.ResName)));
			foreach (var resInfo in msilPlugins.Union(nativePlugins))
			{
				var pluginFileName = Regex.Match(resInfo.ResName, @"^.+?(\w+\.dll)$").Groups[1].Value;
				var pluginFullPath = Path.Combine(pluginsDirectory, pluginFileName);
				loadingLog.AppendFormat("Processing plugin: {0};", pluginFullPath);
				if (!File.Exists(pluginFullPath))
				{
					loadingLog.AppendFormat("Plugin does not exist;");
					var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resInfo.ResName);
					using (var fileStream = new FileStream(pluginFullPath, FileMode.Create))
					{
						resStream.CopyTo(fileStream);
					}
				}
				if (resInfo.IsMSIL)
				{
					Assembly.LoadFrom(pluginFullPath);
				}
			}
		}
	}
}
