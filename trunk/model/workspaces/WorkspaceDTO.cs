using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogJoint.Workspaces
{
    [XmlRoot("workspace")]
    public class WorkspaceDTO
    {
        public string id { get; set; }
        public string annotation { get; set; }
        public bool allowOverwrite { get; set; }
        public List<Source> sources { get; set; }
        public List<EmbeddedStorageEntry> embeddedStorageEntries { get; set; }
        public string entriesArchiveUrl { get; set; }
        public string selfUrl { get; set; }
        public string selfLaunchUrl { get; set; }

        public WorkspaceDTO()
        {
            sources = new List<Source>();
            embeddedStorageEntries = new List<EmbeddedStorageEntry>();
        }

        [XmlType("log-source")]
        public class Source
        {
            public string connectionString { get; set; }
            public bool hidden { get; set; }
        };

        [XmlType("entry")]
        public class EmbeddedStorageEntry
        {
            public string id { get; set; }
            public List<EmbeddedStorageSection> sections { get; set; }

            public EmbeddedStorageEntry()
            {
                sections = new List<EmbeddedStorageSection>();
            }
        };

        [XmlType("section")]
        public class EmbeddedStorageSection
        {
            public string id { get; set; }
            public string value { get; set; }
        };
    }

}
