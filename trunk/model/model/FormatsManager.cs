using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public interface IFormatsRepositoryEntry
	{
		string Location { get; }
		DateTime LastModified { get; }
		XElement LoadFormatDescription();
	};

	public interface IFormatsRepository
	{
		IEnumerable<IFormatsRepositoryEntry> Entries { get; }
	};

	public struct LoadedRegex
	{
		public IRegex Regex;
		public bool SuffersFromPartialMatchProblem;
	};

	public class UserDefinedFormatsManager
	{
		public UserDefinedFormatsManager(IFormatsRepository repository, ILogProviderFactoryRegistry registry)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");
			if (registry == null)
				throw new ArgumentNullException("registry");

			this.repository = repository;
			this.registry = registry;
		}

		public IFormatsRepository Repository
		{
			get { return repository; }
		}

		public static UserDefinedFormatsManager DefaultInstance
		{
			get { return instance; }
		}

		public abstract class UserDefinedFactoryBase: ILogProviderFactory, IDisposable
		{
			public string Location { get { return location; } }
			public DateTime LastChangeTime { get { return lastModified; } }
			public bool IsDisposed { get { return disposed; } }

			public string CompanyName { get { return companyName; } }
			public string FormatName { get { return formatName; } }
			public string FormatDescription { get { return description; } }

			public IFormatViewOptions ViewOptions { get { return viewOptions; } }

			public abstract ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory);
			public abstract string GetUserFriendlyConnectionName(IConnectionParams connectParams);
			public abstract IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams);
			public abstract ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams);

			public string GetConnectionId(IConnectionParams connectParams)
			{
				return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
			}

			public struct CreateParams
			{
				public ILogProviderFactoryRegistry FactoryRegistry;
				public IFormatsRepositoryEntry Entry;
				public XElement RootNode;
				public XElement FormatSpecificNode;
			};

			public UserDefinedFactoryBase(CreateParams createParams)
			{
				if (createParams.FormatSpecificNode == null)
					throw new ArgumentNullException("createParams.FormatSpecificNode");
				if (createParams.RootNode == null)
					throw new ArgumentNullException("createParams.RootNode");

				if (createParams.Entry != null)
				{
					this.location = createParams.Entry.Location;
					this.lastModified = createParams.Entry.LastModified;
				}

				this.factoryRegistry = createParams.FactoryRegistry;

				var idData = createParams.RootNode.Elements("id").Select(
					id => new { company = id.AttributeValue("company"), formatName = id.AttributeValue("name")}).FirstOrDefault();

				if (idData != null)
				{
					companyName = idData.company;
					formatName = idData.formatName;
				}

				description = ReadParameter(createParams.RootNode, "description").Trim();

				viewOptions = new FormatViewOptions(createParams.RootNode.Element("view-options"));

				if (factoryRegistry != null)
					factoryRegistry.Register(this);
			}

			public override string ToString()
			{
				return LogProviderFactoryRegistry.ToString(this);
			}

			#region IDisposable Members

			public void Dispose()
			{
				if (disposed)
					return;
				disposed = true;
				if (factoryRegistry != null)
					factoryRegistry.Unregister(this);
			}

			#endregion

			protected static string ReadParameter(XElement root, string name)
			{
				return root.Elements(name).Select(a => a.Value).FirstOrDefault() ?? "";
			}

			protected static LoadedRegex ReadRe(XElement root, string name, ReOptions opts)
			{
				LoadedRegex ret = new LoadedRegex();
				var n = root.Element(name);
				if (n == null)
					return ret;
				string pattern = n.Value;
				if (string.IsNullOrEmpty(pattern))
					return ret;
				ret.Regex = RegexFactory.Instance.Create(pattern, opts);
				XAttribute attr;
				ret.SuffersFromPartialMatchProblem = 
					(attr = n.Attribute("suffers-from-partial-match-problem")) != null 
					&& attr.Value == "yes";
				return ret;
			}

			protected static Type ReadType(XElement root, string name, Type defType)
			{
				string typeName = ReadParameter(root, name);
				if (string.IsNullOrEmpty(typeName))
					return defType;
				return Type.GetType(typeName);
			}

			protected static Type ReadPrecompiledUserCode(XElement root)
			{
				var codeNode = root.Element("precompiled-user-code");
				if (codeNode == null)
					return null;
				var typeAttr = codeNode.Attribute("type");
				if (typeAttr == null)
					return null;
				Assembly asm;
				byte[] asmBytes = Convert.FromBase64String(codeNode.Value);
#if !SILVERLIGHT
				asm = Assembly.Load(asmBytes);
#else
				var asmPart = new System.Windows.AssemblyPart();
				asm = asmPart.Load(new MemoryStream(asmBytes));
#endif
				return asm.GetType(typeAttr.Value);
			}

			protected static void ReadPatterns(XElement formatSpecificNode, List<string> patternsList)
			{
				patternsList.AddRange(
					from patterns in formatSpecificNode.Elements("patterns")
					from pattern in patterns.Elements("pattern")
					let patternVal = pattern.Value
					where patternVal != ""
					select patternVal);
			}

			readonly string location;
			readonly DateTime lastModified;
			readonly string companyName;
			readonly string formatName;
			readonly string description = "";
			readonly ILogProviderFactoryRegistry factoryRegistry;
			readonly FormatViewOptions viewOptions;
			internal bool entryExists;
			bool disposed;
		};

		public void RegisterFormatType(string configNodeName, Type formatType)
		{
			if (string.IsNullOrEmpty(configNodeName))
				throw new ArgumentException("Node name must be a not-null not-empty string", "formatConfigType");

			if (!typeof(UserDefinedFactoryBase).IsAssignableFrom(formatType))
				throw new ArgumentException("Type must be inherited from " + typeof(UserDefinedFactoryBase).Name, "formatType");

			nodeNameToType.Add(configNodeName, formatType);
		}

		public int ReloadFactories()
		{
			int ret = 0;

			MarkAllFactoriesAsNonExisting();

			foreach (IFormatsRepositoryEntry entry in repository.Entries)
			{
				string location = entry.Location;
				UserDefinedFactoryBase factory = factories.Where(f => f.Location == location).FirstOrDefault();
				if (factory != null
					&& factory.LastChangeTime == entry.LastModified)
				{
					factory.entryExists = true;
					continue;
				}
				factory = LoadFactory(entry);
				if (factory != null)
				{
					factory.entryExists = true;
					factories.Add(factory);
				}
				++ret;
			}

			ret += DeleteNotExistingFactories();

			return ret;
		}

		public IEnumerable<UserDefinedFactoryBase> Items
		{
			get
			{
				return factories;
			}
		}


		void MarkAllFactoriesAsNonExisting()
		{
			foreach (UserDefinedFactoryBase f in factories)
			{
				f.entryExists = false;
			}
		}

		int DeleteNotExistingFactories()
		{
			foreach (UserDefinedFactoryBase f in factories)
			{
				if (!f.entryExists)
					f.Dispose();
			}
			return ListUtils.RemoveAll(factories, f => !f.entryExists);
		}

		UserDefinedFactoryBase LoadFactory(IFormatsRepositoryEntry entry)
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
					RootNode = root,
				}
				select (UserDefinedFactoryBase)Activator.CreateInstance(
					nodeNameToType[factoryNodeCandidate.Name.LocalName], createParams)
			).FirstOrDefault();
		}

#if !SILVERLIGHT
		readonly static UserDefinedFormatsManager instance = new UserDefinedFormatsManager(
			DirectoryFormatsRepository.DefaultRepository, LogProviderFactoryRegistry.DefaultInstance);
#else
		readonly static UserDefinedFormatsManager instance = 
			new UserDefinedFormatsManager(new ResourcesFormatsRepository(Assembly.GetExecutingAssembly()), 
				LogProviderFactoryRegistry.DefaultInstance);
#endif
		readonly IFormatsRepository repository;
		readonly ILogProviderFactoryRegistry registry;
		readonly Dictionary<string, Type> nodeNameToType = new Dictionary<string, Type>();
		readonly List<UserDefinedFactoryBase> factories = new List<UserDefinedFactoryBase>();
	}

#if !SILVERLIGHT
	public class DirectoryFormatsRepository : IFormatsRepository
	{
		public DirectoryFormatsRepository(string directoryPath)
		{
			this.directoryPath = string.IsNullOrEmpty(directoryPath) ? DefaultRepositoryLocation : directoryPath;
		}

		public string RepositoryLocation
		{
			get { return directoryPath; }
		}

		public static string DefaultRepositoryLocation
		{
			get
			{
				return Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().Location) + @"\Formats";
			}
		}

		public static DirectoryFormatsRepository DefaultRepository
		{
			get { return instance; }
		}

		public string GetFullFormatFileName(string nameBasis)
		{
			return string.Format("{0}\\{1}.format.xml", RepositoryLocation, nameBasis);
		}

		public IEnumerable<IFormatsRepositoryEntry> Entries
		{
			get 
			{
				if (Directory.Exists(directoryPath))
				{
					foreach (string fname in Directory.GetFiles(directoryPath, "*.format.xml"))
					{
						yield return new Entry(fname);
					}
				}
			}
		}

		class Entry: IFormatsRepositoryEntry
		{
			internal Entry(string fileName)
			{
				this.fileName = fileName;
			}

			public string Location
			{
				get { return fileName; }
			}

			public DateTime LastModified
			{
				get { return File.GetLastWriteTime(fileName); }
			}

			public XElement LoadFormatDescription()
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Open))
					return XDocument.Load(fs).Element("format");
			}

			readonly string fileName;
		};

		readonly string directoryPath;
		static readonly DirectoryFormatsRepository instance = new DirectoryFormatsRepository(null);
	};
#endif

	public class ResourcesFormatsRepository: IFormatsRepository
	{
		public ResourcesFormatsRepository(Assembly resourcesAssembly)
		{
			if (resourcesAssembly == null)
				throw new ArgumentNullException("resourcesAssembly");
			this.resourcesAssembly = resourcesAssembly;
		}

		public IEnumerable<IFormatsRepositoryEntry> Entries
		{
			get 
			{
				foreach (string resourceName in resourcesAssembly.GetManifestResourceNames().Where((f) => f.EndsWith(".format.xml")))
				{
					yield return new Entry(resourcesAssembly, resourceName);
				}
			}
		}

		class Entry: IFormatsRepositoryEntry
		{
			internal Entry(Assembly resourcesAssembly, string resourceName)
			{
				this.resourcesAssembly = resourcesAssembly;
				this.resourceName = resourceName;
			}

			public string Location
			{
				get { return resourceName; }
			}

			public DateTime LastModified
			{
				get { return new DateTime(); }
			}

			public XElement LoadFormatDescription()
			{
				return XDocument.Load(resourcesAssembly.GetManifestResourceStream(resourceName)).Element("format");
			}

			readonly string resourceName;
			readonly Assembly resourcesAssembly;
		};

		readonly Assembly resourcesAssembly;
	};
}
