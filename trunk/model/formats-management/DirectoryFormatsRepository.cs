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
        public DirectoryFormatsRepository(string directoryPath, string[] additionalDirectories = null)
        {
            this.directoryPaths.Add(string.IsNullOrEmpty(directoryPath) ? DefaultRepositoryLocation : directoryPath);
            if (additionalDirectories != null)
                this.directoryPaths.AddRange(additionalDirectories);
        }

        public string RepositoryLocation => directoryPaths[0];

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
                foreach (string directoryPath in directoryPaths)
                {
                    if (Directory.Exists(directoryPath))
                    {
                        foreach (string fullFileName in Directory.GetFiles(directoryPath, "*.format.xml"))
                        {
                            var fname = Path.GetFileName(fullFileName);
                            if (string.Compare(fname, "Skype - Caf‚ Log.format.xml", ignoreCase: true) == 0
                             || string.Compare(fname, "Skype - Café Log.format.xml", ignoreCase: true) == 0
                             || string.Compare(fname, "Skype - Caf%E9 Log.format.xml", ignoreCase: true) == 0)
                            {
                                // todo: dirty hack. intro configurable blacklist instead.
                                continue;
                            }
                            yield return new Entry(fullFileName);
                        }
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
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    return XDocument.Load(fs).Element("format");
            }

            readonly string fileName;
        };

        readonly List<string> directoryPaths = new List<string>();
    };
}
