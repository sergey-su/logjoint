using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace LogJoint
{
	public static class PluginsManagerExtensions
	{
		public static void ValidateFilesExist(this IPluginManifest pluginManifest)
		{
			var missingFiles = string.Join(",", pluginManifest.Files.Where(f => !File.Exists(f.AbsolulePath)).Select(f => f.RelativePath));
			if (missingFiles.Length > 0)
				throw new FileNotFoundException($"Some plugin files are missing: {missingFiles}");
		}
	};
}
