using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	public class UserDefinedFormatsManager : IUserDefinedFormatsManager
	{
		public UserDefinedFormatsManager(IFormatDefinitionsRepository repository, ILogProviderFactoryRegistry registry)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");
			if (registry == null)
				throw new ArgumentNullException("registry");

			this.repository = repository;
			this.registry = registry;
		}

		IFormatDefinitionsRepository IUserDefinedFormatsManager.Repository
		{
			get { return repository; }
		}

		void IUserDefinedFormatsManager.RegisterFormatType(string configNodeName, Type formatType)
		{
			if (string.IsNullOrEmpty(configNodeName))
				throw new ArgumentException("Node name must be a not-null not-empty string", "formatConfigType");

			if (!typeof(UserDefinedFactoryBase).IsAssignableFrom(formatType))
				throw new ArgumentException("Type must be inherited from " + typeof(UserDefinedFactoryBase).Name, "formatType");

			nodeNameToType.Add(configNodeName, formatType);
		}

		int IUserDefinedFormatsManager.ReloadFactories()
		{
			int ret = 0;

			MarkAllFactoriesAsNonExisting();

			foreach (IFormatDefinitionRepositoryEntry entry in repository.Entries)
			{
				string location = entry.Location;
				IUserDefinedFactory factory = factories.Where(f => f.Location == location).FirstOrDefault();
				if (factory != null
					&& factory.LastModified == entry.LastModified)
				{
					factory.FactoryExists = true;
					continue;
				}
				factory = LoadFactory(entry);
				if (factory != null)
				{
					factory.FactoryExists = true;
					factories.Add(factory);
				}
				++ret;
			}

			ret += DeleteNotExistingFactories();

			return ret;
		}

		IEnumerable<IUserDefinedFactory> IUserDefinedFormatsManager.Items
		{
			get
			{
				return factories;
			}
		}


		void MarkAllFactoriesAsNonExisting()
		{
			foreach (IUserDefinedFactory f in factories)
			{
				f.FactoryExists = false;
			}
		}

		int DeleteNotExistingFactories()
		{
			foreach (IUserDefinedFactory f in factories)
			{
				if (!f.FactoryExists)
					f.Dispose();
			}
			return ListUtils.RemoveAll(factories, f => !f.FactoryExists);
		}

		IUserDefinedFactory LoadFactory(IFormatDefinitionRepositoryEntry entry)
		{
			var root = entry.LoadFormatDescription();
			return (
				from factoryNodeCandidate in root.Elements()
				where nodeNameToType.ContainsKey(factoryNodeCandidate.Name.LocalName)
				let createParams = new UserDefinedFactoryBase.CreateParams()
				{
					Entry = entry,
					FactoryRegistry = registry,
					FormatSpecificNode = factoryNodeCandidate,
					RootNode = root
				}
				select (IUserDefinedFactory)Activator.CreateInstance(
					nodeNameToType[factoryNodeCandidate.Name.LocalName], createParams)
			).FirstOrDefault();
		}

#if !SILVERLIGHT
#else
		readonly static UserDefinedFormatsManager instance = 
			new UserDefinedFormatsManager(new ResourcesFormatsRepository(Assembly.GetExecutingAssembly()), 
				LogProviderFactoryRegistry.DefaultInstance);
#endif
		readonly IFormatDefinitionsRepository repository;
		readonly ILogProviderFactoryRegistry registry;
		readonly Dictionary<string, Type> nodeNameToType = new Dictionary<string, Type>();
		readonly List<IUserDefinedFactory> factories = new List<IUserDefinedFactory>();
	}
}
