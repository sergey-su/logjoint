using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LogJoint.Wasm.Extensibility
{
    public static class BlazorPluginLoader
    {
        public static void LoadPlugins(WebAssemblyHost wasmHost)
        {
            var pluginsDirsList = new List<string>();
            var pluginsDir = Path.Combine(Path.GetTempPath(), "plugins"); // folder in memory, powered by emscripten MEMFS.
            var resourcesAssembly = Assembly.GetExecutingAssembly();

            foreach (string resourceName in resourcesAssembly.GetManifestResourceNames().Where(f => f.StartsWith("LogJoint.Wasm.Plugins")))
            {
                var resourceStream = resourcesAssembly.GetManifestResourceStream(resourceName);
                var pluginDir = Path.Combine(pluginsDir, resourceName);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                using (var archive = new ZipArchive(resourceStream, ZipArchiveMode.Read, leaveOpen: true))
                {
                    var createdDirectories = new HashSet<string>();
                    void ensureDirectoryCreated(string dir)
                    {
                        if (createdDirectories.Add(dir))
                            Directory.CreateDirectory(dir);
                    };
                    foreach (var e in archive.Entries)
                    {
                        var fileName = Path.Combine(pluginDir, e.FullName);
                        ensureDirectoryCreated(Path.GetDirectoryName(fileName));
                        using (var sourceStream = e.Open())
                        using (var targetStream = File.OpenWrite(fileName))
                        {
                            sourceStream.CopyTo(targetStream);
                        }
                    }
                }
                Console.WriteLine("Extracted plugin: {0}, took {1}", resourceName, sw.Elapsed);
                pluginsDirsList.Add(pluginDir);
            }

            var model = wasmHost.Services.GetService<LogJoint.ModelObjects>();
            var presentation = wasmHost.Services.GetService<LogJoint.UI.Presenters.PresentationObjects>();
            model.PluginsManager.LoadPlugins(new Extensibility.Application(
                model.ExpensibilityEntryPoint,
                presentation.ExpensibilityEntryPoint), string.Join(',', pluginsDirsList), false);
        }
    }
}
