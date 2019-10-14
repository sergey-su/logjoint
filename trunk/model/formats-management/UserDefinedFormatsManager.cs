using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LogJoint
{
	public class UserDefinedFormatsManager : IUserDefinedFormatsManager, IPluginFormatsManager
	{
		public UserDefinedFormatsManager(
			IFormatDefinitionsRepository repository,
			ILogProviderFactoryRegistry registry,
			ITempFilesManager tempFilesManager,
			ITraceSourceFactory traceSourceFactory,
			RegularExpressions.IRegexFactory regexFactory,
			FieldsProcessor.IFactory fieldsProcessorFactory
		)
		{
			this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
			this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
			this.tempFilesManager = tempFilesManager;
			this.traceSourceFactory = traceSourceFactory;
			this.regexFactory = regexFactory;
			this.fieldsProcessorFactory = fieldsProcessorFactory;
			this.tracer = traceSourceFactory.CreateTraceSource("UserDefinedFormatsManager", "udfm");
		}

		IFormatDefinitionsRepository IUserDefinedFormatsManager.Repository
		{
			get { return repository; }
		}

		void IUserDefinedFormatsManager.RegisterFormatType(string configNodeName, Type formatType)
		{
			if (string.IsNullOrEmpty(configNodeName))
				throw new ArgumentException("Node name must be a not-null not-empty string", "formatConfigType");

			if (!typeof(IUserDefinedFactory).IsAssignableFrom(formatType))
				throw new ArgumentException("Type must be inherited from " + typeof(IUserDefinedFactory).Name, "formatType");

			nodeNameToType.Add(configNodeName, formatType);
		}

		int IUserDefinedFormatsManager.ReloadFactories()
		{
			tracer.Info("reloading factories");
			int ret = 0;

			MarkAllFactoriesAsNonExisting();

			foreach (IFormatDefinitionRepositoryEntry entry in repository.Entries)
			{
				string location = entry.Location;
				var factory = factories.Where(f => f.factory.Location == location).FirstOrDefault();
				if (factory != null
				 && factory.lastModified == entry.LastModified)
				{
					factory.markedForDeletion = false;
					tracer.Info("factory '{0}' did not change", location);
					continue;
				}
				tracer.Info("factory '{0}' needs (re)loading", location);
				factory = LoadFactory(entry);
				if (factory != null)
				{
					factory.markedForDeletion = false;
					factories.Add(factory);
				}
				++ret;
			}

			ret += DeleteNotExistingFactories();

			tracer.Info("factories changed: {0}", ret);

			return ret;
		}

		IEnumerable<IUserDefinedFactory> IUserDefinedFormatsManager.Items
		{
			get
			{
				return factories.Select(f => f.factory).Union(pluginFactories);
			}
		}

		void IPluginFormatsManager.RegisterPluginFormats(Extensibility.IPluginManifest manifest)
		{
			foreach (var formatFile in manifest.Files.Where(f => f.Type == Extensibility.PluginFileType.FormatDefinition))
			{
				var root = XDocument.Load(formatFile.AbsolulePath).Element("format");
				pluginFactories.AddRange(
					from factoryNodeCandidate in root.Elements()
					where nodeNameToType.ContainsKey(factoryNodeCandidate.Name.LocalName)
					let createParams = new UserDefinedFactoryParams()
					{
						Location = formatFile.AbsolulePath,
						FactoryRegistry = registry,
						TempFilesManager = tempFilesManager,
						TraceSourceFactory = traceSourceFactory,
						RegexFactory = regexFactory,
						FieldsProcessorFactory = fieldsProcessorFactory,
						FormatSpecificNode = factoryNodeCandidate,
						RootNode = root
					}
					select (IUserDefinedFactory)Activator.CreateInstance(
							nodeNameToType[factoryNodeCandidate.Name.LocalName], createParams)
				);
			}
		}

		void MarkAllFactoriesAsNonExisting()
		{
			foreach (var f in factories)
			{
				f.markedForDeletion = true;
			}
		}

		int DeleteNotExistingFactories()
		{
			foreach (var f in factories)
			{
				if (f.markedForDeletion)
				{
					tracer.Info("factory '{0}' does not exist anymore. disposing it.", f.factory.Location);
					f.factory.Dispose();
				}
			}
			return ListUtils.RemoveAll(factories, f => f.markedForDeletion);
		}

		FactoryRecord LoadFactory(IFormatDefinitionRepositoryEntry entry)
		{
			var root = entry.LoadFormatDescription();
			return (
				from factoryNodeCandidate in root.Elements()
				where nodeNameToType.ContainsKey(factoryNodeCandidate.Name.LocalName)
				let createParams = new UserDefinedFactoryParams()
				{
					Location = entry.Location,
					FactoryRegistry = registry,
					TempFilesManager = tempFilesManager,
					TraceSourceFactory = traceSourceFactory,
					RegexFactory = regexFactory,
					FieldsProcessorFactory = fieldsProcessorFactory,
					FormatSpecificNode = factoryNodeCandidate,
					RootNode = root
				}
				select new FactoryRecord()
				{
					factory = (IUserDefinedFactory)Activator.CreateInstance(
						nodeNameToType[factoryNodeCandidate.Name.LocalName], createParams),
					lastModified = entry.LastModified,
					markedForDeletion = false
				}
			).FirstOrDefault();
		}

		class FactoryRecord
		{
			public IUserDefinedFactory factory;
			public DateTime lastModified;
			public bool markedForDeletion;
		};

		readonly IFormatDefinitionsRepository repository;
		readonly ILogProviderFactoryRegistry registry;
		readonly ITempFilesManager tempFilesManager;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly RegularExpressions.IRegexFactory regexFactory;
		readonly FieldsProcessor.IFactory fieldsProcessorFactory;
		readonly LJTraceSource tracer;
		readonly Dictionary<string, Type> nodeNameToType = new Dictionary<string, Type>();
		readonly List<FactoryRecord> factories = new List<FactoryRecord>();
		readonly List<IUserDefinedFactory> pluginFactories = new List<IUserDefinedFactory>();
	}
}
