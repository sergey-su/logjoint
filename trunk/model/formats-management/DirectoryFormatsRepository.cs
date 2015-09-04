using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace LogJoint
{
	public class DirectoryFormatsRepository : IFormatDefinitionsRepository
	{
		public DirectoryFormatsRepository(string directoryPath)
		{
			this.directoryPath = string.IsNullOrEmpty(directoryPath) ? DefaultRepositoryLocation : directoryPath;
		}

		public string RepositoryLocation
		{
			get { return directoryPath; }
		}

		public static string RelativeFormatsLocation
		{
			get { return "Formats"; }
		}

		public static string DefaultRepositoryLocation
		{
			get
			{
				return Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + RelativeFormatsLocation;
			}
		}

		public string GetFullFormatFileName(string nameBasis)
		{
			return string.Format("{0}{2}{1}.format.xml", RepositoryLocation, nameBasis, Path.DirectorySeparatorChar);
		}

		public IEnumerable<IFormatDefinitionRepositoryEntry> Entries
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

		class Entry : IFormatDefinitionRepositoryEntry
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
	};
}
