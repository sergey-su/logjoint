using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace LogJoint.Analytics.TimeSeries
{
	public class TimeSeriesTypesLoader : ITimeSeriesTypesAccess
	{
		string customConfigEnvVar;
		Assembly defaultTimeSeriesTypesAssembly = Assembly.GetExecutingAssembly();
		readonly object sync = new object();
		XmlSerializer eventsSerializer;
		XmlSerializer seriesSerializer;
		Metadata metadataCache;
		long lastCustomConfigUpdateCheck = Environment.TickCount;

		Assembly ITimeSeriesTypesAccess.DefaultTimeSeriesTypesAssembly
		{
			get { return defaultTimeSeriesTypesAssembly; }
			set { defaultTimeSeriesTypesAssembly = value; }
		}

		void ITimeSeriesTypesAccess.CheckForCustomConfigUpdate()
		{
			// update check does not reload anything.
			// it only invalidates cached data if it detects a change that potentially affects custom config.
			lock (sync)
			{
				if (metadataCache == null)
					return; // config cache is already invalidated

				var timestamp = Environment.TickCount;
				if ((timestamp - lastCustomConfigUpdateCheck) < 5000)
					return; // do not check for updates more often that once in X seconds
				lastCustomConfigUpdateCheck = timestamp;

				if (metadataCache.customSourceFile == null) // if currently no custom config is loaded from user file
				{
					// invalidate cache if any of custom config locations contains a (new) file
					if (GetUserDefinedParserConfigPaths(customConfigEnvVar).Where(File.Exists).Any())
					{
						InvalidateMetadataCache();
					}
				}
				else // if currently some types are loaded from custom config
				{
					// invalidate cache if custom config file is gone or changed
					var fi = new FileInfo(metadataCache.customSourceFile);
					if (!fi.Exists || fi.LastWriteTimeUtc != metadataCache.customSourceFileLastModified)
					{
						InvalidateMetadataCache();
					}
				}
			}
		}

		IEnumerable<Type> ITimeSeriesTypesAccess.GetMetadataTypes()
		{
			lock (sync)
			{
				return GetMetadata().types;
			}
		}

		XmlSerializer ITimeSeriesTypesAccess.GetEventsSerializer()
		{
			lock (sync)
			{
				if (eventsSerializer == null)
					eventsSerializer = CreateEventsSerializer();
				return eventsSerializer;
			}
		}

		XmlSerializer ITimeSeriesTypesAccess.GetSeriesSerializer()
		{
			lock (sync)
			{
				if (seriesSerializer == null)
					seriesSerializer = CreateSeriesSerializer();
				return seriesSerializer;
			}
		}

		string ITimeSeriesTypesAccess.UserDefinedParserConfigPath
		{
			get { return GetUserDefinedParserConfigPath(); }
		}

		string ITimeSeriesTypesAccess.CustomConfigLoadingError
		{
			get
			{
				lock (sync)
				{
					return GetMetadata().customConfigLoadingError;
				}
			}
		}

		string ITimeSeriesTypesAccess.CustomConfigEnvVar
		{
			get { return customConfigEnvVar; }
			set { customConfigEnvVar = value; }
		}

		public static string GetFullSourceFilePath()
		{
			return GetFullSourceFilePathImpl();
		}

		static Metadata TryLoadFromCustomPath(string path)
		{
			if (File.Exists(path))
			{
				var loader = new DynamicScriptLoader();
				var result = loader.Load(new FileInfo(path), false, new string[] { Assembly.GetAssembly(typeof(Event)).Location });
				return new Metadata()
				{
					customAssembly = result,
					customSourceFile = path,
					customSourceFileLastModified = File.GetLastWriteTimeUtc(path),
				};
			}
			return null;
		}

		Metadata GetMetadata()
		{
			if (metadataCache == null)
				metadataCache = CreateMetadata(this.customConfigEnvVar, this.defaultTimeSeriesTypesAssembly);
			return metadataCache;
		}

		static IEnumerable<string> GetUserDefinedParserConfigPaths(string evnVar)
		{
			var envPathOverride = evnVar != null ? Environment.GetEnvironmentVariable(evnVar) : null;
			if (envPathOverride != null)
				yield return envPathOverride;
			yield return GetUserDefinedParserConfigPath();
		}

		static Metadata CreateMetadata(string evnVar, Assembly defaultTimeSeriesTypesAssembly)
		{
			Metadata asm = null;

			var customConfigLoadingError = new StringBuilder();
			foreach (var customPath in GetUserDefinedParserConfigPaths(evnVar))
			{
				try
				{
					if ((asm = TryLoadFromCustomPath(customPath)) != null)
						break;
				}
				catch (Exception e)
				{
					customConfigLoadingError.AppendFormat("Failed to load custom config from '{0}'.", customPath);
					customConfigLoadingError.AppendLine();
					customConfigLoadingError.AppendFormat("{0}: {1}", e.GetType(), e.Message);
				}
			}

			if (asm == null)
				asm = new Metadata();

			asm.baseAssembly = defaultTimeSeriesTypesAssembly;
			asm.customConfigLoadingError = customConfigLoadingError.ToString();

			Func<Assembly, IEnumerable<Type>> getAsmTypes = a =>
				a == null ?
					Enumerable.Empty<Type>() :
					a.GetTypes().Where(t =>
						t.IsClass &&
						(t.GetCustomAttributes<TimeSeriesEventAttribute>().Any() || t.GetCustomAttributes<EventAttribute>().Any())
					);

			var typesDict = new Dictionary<string, Type>();
			foreach (var i in
				getAsmTypes(asm.baseAssembly)
				.Union(getAsmTypes(asm.customAssembly)) // adding user-defined types to the end of sequence; they will overwrite predefined ones in case of conflicting names
			)
			{
				typesDict[i.Name] = i;
			}

			asm.types = typesDict.Values.ToList();

			return asm;
		}

		private static string GetUserDefinedParserConfigPath()
		{
			var pluginDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var customScriptPath = Path.Combine(pluginDir, "ParserConfig.cs");
			return customScriptPath;
		}

		private XmlSerializer CreateEventsSerializer()
		{
			var baseEventType = typeof(Event);
			var extraTypes = GetMetadata().types.Where(baseEventType.IsAssignableFrom).ToArray();
			return new XmlSerializer(typeof(List<Event>), extraTypes);
		}

		private XmlSerializer CreateSeriesSerializer()
		{
			var baseEventType = typeof(TimeSeries);
			var extraTypes = GetMetadata().types.Where(baseEventType.IsAssignableFrom).ToArray();
			return new XmlSerializer(typeof(List<TimeSeries>), extraTypes.ToArray());
		}

		private static string GetFullSourceFilePathImpl([CallerFilePath] string file = "")
		{
			return file;
		}

		private void InvalidateMetadataCache()
		{
			metadataCache = null;
			eventsSerializer = null;
			seriesSerializer = null;
		}

		class Metadata
		{
			public Assembly baseAssembly;
			public Assembly customAssembly;
			public string customSourceFile;
			public DateTime customSourceFileLastModified;
			public List<Type> types;
			public string customConfigLoadingError;
		};
	}

	class DynamicScriptLoader
	{
		public Assembly Load(FileInfo sourceFile, bool debugInfo, IEnumerable<string> dependencies = null)
		{
			return Load(new FileInfo[] { sourceFile }, debugInfo, dependencies);
		}

		public Assembly Load(FileInfo[] sourceFiles, bool debugInfo, IEnumerable<string> dependencies = null)
		{
			using (var compiler = new CSharpCodeProvider())
			{
				var compilerParams = new CompilerParameters()
				{
					TreatWarningsAsErrors = false,
					GenerateExecutable = false,
					GenerateInMemory = true,
					IncludeDebugInformation = false
				};

				if (debugInfo)
				{
					// To be able to debug the dynamically compiled assembly in debug mode, we have to change some compiler flags
					compilerParams.GenerateInMemory = false;
					compilerParams.IncludeDebugInformation = true;
					compilerParams.OutputAssembly = "Custom.TimeSeries." + Guid.NewGuid().ToString() + ".dll";
				}

				compilerParams.ReferencedAssemblies.AddRange(new string[] { "mscorlib.dll", "System.Core.dll", "Microsoft.CSharp.dll", "System.dll" });

				if (dependencies != null)
				{
					compilerParams.ReferencedAssemblies.AddRange(dependencies.ToArray());
				}

				var results = compiler.CompileAssemblyFromFile(compilerParams, sourceFiles.Select(f => f.FullName).ToArray());

				if (results.Errors.HasErrors)
				{
					throw new Exception(string.Format("Compile Errors:\n {0}", string.Join("\n", results.Errors.Cast<object>().ToArray())));
				}
				return results.CompiledAssembly;
			}
		}
	}
}
