using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public class UserDefinedFormatsManager
	{
		public static UserDefinedFormatsManager Instance
		{
			get { return instance; }
		}

		public abstract class UserDefinedFactoryBase: ILogReaderFactory, IDisposable
		{
			public string FileName { get { return fileName; } }
			public DateTime LastChangeTime { get { return lastModified; } }
			public bool IsDisposed { get { return disposed; } }

			public string CompanyName { get { return companyName; } }
			public string FormatName { get { return formatName; } }
			public string FormatDescription { get { return description; } }

			public abstract ILogReaderFactoryUI CreateUI();
			public abstract string GetUserFriendlyConnectionName(IConnectionParams connectParams);
			public abstract ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams);

			public UserDefinedFactoryBase(string fileName, XmlNode rootNode, XmlNode formatSpecificNode)
			{
				this.fileName = fileName;
				if (fileName != null)
				{
					this.lastModified = File.GetLastWriteTime(fileName);
				}

				XmlNode n;

				if ((n = rootNode.SelectSingleNode("id/@company")) != null)
					companyName = n.Value;

				if ((n = rootNode.SelectSingleNode("id/@name")) != null)
					formatName = n.Value;

				if ((n = rootNode.SelectSingleNode("description")) != null)
					description = n.InnerText.Trim();

				LogReaderFactoryRegistry.Instance.Register(this);
			}

			public override string ToString()
			{
				return LogReaderFactoryRegistry.ToString(this);
			}

			#region IDisposable Members

			public void Dispose()
			{
				if (disposed)
					return;
				disposed = true;
				LogReaderFactoryRegistry.Instance.Unregister(this);
			}

			#endregion

			protected static string ReadParameter(XmlNode root, string name)
			{
				XmlNode n = root.SelectSingleNode(name + "/text()");
				if (n == null)
					return "";
				else
					return n.Value;
			}

			protected static Regex ReadRe(XmlNode root, string name, RegexOptions opts)
			{
				string s = ReadParameter(root, name);
				if (string.IsNullOrEmpty(s))
					return null;
				return new Regex(s, opts | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
			}

			protected static void ReadPatterns(XmlNode formatSpecificNode, List<string> patterns)
			{
				foreach (XmlNode n in formatSpecificNode.SelectNodes("patterns/pattern[text()!='']"))
					patterns.Add(n.InnerText);
			}

			string fileName;
			DateTime lastModified;
			bool disposed;
			string companyName;
			string formatName;
			string description = "";
			internal bool fileExists;
		};

		public void RegisterFormatType(string configNodeName, Type formatConfigType)
		{
			if (string.IsNullOrEmpty(configNodeName))
				throw new ArgumentException("Node name must be a not-null not-empty string", "formatConfigType");

			if (!typeof(UserDefinedFactoryBase).IsAssignableFrom(formatConfigType))
				throw new ArgumentException("Type must be inherited from FormatConfig", "formatConfigType");

			nodeNameToType.Add(configNodeName, formatConfigType);
		}

		public string RepositoryLocation
		{
			get
			{
				return Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().Location) + @"\Formats";
			}
		}

		public string GetFullFormatFileName(string nameBasis)
		{
			return string.Format("{0}\\{1}.format.xml", RepositoryLocation,	nameBasis);
		}

		public int ReloadFactories()
		{
			int ret = 0;

			MarkAllFactoriesAsNonExisting();

			foreach (string fname in Directory.GetFiles(RepositoryLocation, "*.format.xml"))
			{
				UserDefinedFactoryBase factory = factories.Find((Predicate<UserDefinedFactoryBase>)
					delegate(UserDefinedFactoryBase f) { return f.FileName == fname; });
				if (factory != null 
				 && factory.LastChangeTime == File.GetLastWriteTime(fname))
				{
					factory.fileExists = true;
					continue;
				}
				factory = LoadFactory(fname);
				if (factory != null)
				{
					factory.fileExists = true;
					factories.Add(factory);
				}
				++ret;
			}

			ret += DeleteNonExistingFactories();

			return ret;
		}

		void MarkAllFactoriesAsNonExisting()
		{
			foreach (UserDefinedFactoryBase f in factories)
			{
				f.fileExists = false;
			}
		}

		int DeleteNonExistingFactories()
		{
			foreach (UserDefinedFactoryBase f in factories)
			{
				if (!f.fileExists)
					f.Dispose();
			}
			return factories.RemoveAll((Predicate<UserDefinedFactoryBase>)
				delegate(UserDefinedFactoryBase f) { return !f.fileExists; });
		}

		public IEnumerable<UserDefinedFactoryBase> Items
		{
			get
			{
				return factories;
			}
		}

		string MakeFormatNodeQuery()
		{
			StringBuilder formatNodeQuery = new StringBuilder();
			foreach (string nodeName in nodeNameToType.Keys)
			{
				if (formatNodeQuery.Length != 0)
					formatNodeQuery.Append(" | ");
				formatNodeQuery.Append(nodeName);
			}
			return formatNodeQuery.ToString();
		}

		UserDefinedFactoryBase LoadFactory(string fname)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(fname);
			XmlElement n = (XmlElement)doc.DocumentElement.SelectSingleNode(MakeFormatNodeQuery());
			if (n == null)
				return null;
			return (UserDefinedFactoryBase)Activator.CreateInstance(
				nodeNameToType[n.Name], fname, doc.DocumentElement, n);
		}

		readonly static UserDefinedFormatsManager instance = new UserDefinedFormatsManager();
		Dictionary<string, Type> nodeNameToType = new Dictionary<string, Type>();
		List<UserDefinedFactoryBase> factories = new List<UserDefinedFactoryBase>();
	}
}
