using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LogJoint
{
    public class ResourcesFormatsRepository : IFormatDefinitionsRepository
    {
        public ResourcesFormatsRepository(Assembly resourcesAssembly)
        {
            if (resourcesAssembly == null)
                throw new ArgumentNullException(nameof(resourcesAssembly));
            this.resourcesAssembly = resourcesAssembly;
        }

        public IEnumerable<IFormatDefinitionRepositoryEntry> Entries
        {
            get
            {
                foreach (string resourceName in resourcesAssembly.GetManifestResourceNames().Where((f) => f.EndsWith(".format.xml")))
                {
                    yield return new Entry(resourcesAssembly, resourceName);
                }
            }
        }

        class Entry : IFormatDefinitionRepositoryEntry
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
