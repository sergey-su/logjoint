using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Xml.Linq;

namespace LogJoint.AutoUpdate
{
    struct UpdateInfoFileContent
    {
        public string BinariesETag;
        public DateTime? LastCheckTimestamp;
        public string LastCheckError;

        public UpdateInfoFileContent(string binariesETag, DateTime? lastCheckTimestamp, string lastCheckError)
        {
            BinariesETag = binariesETag;
            LastCheckTimestamp = lastCheckTimestamp;
            LastCheckError = lastCheckError;
        }

        public static UpdateInfoFileContent Read(string fileName)
        {
            var retVal = new UpdateInfoFileContent();
            if (File.Exists(fileName))
            {
                try
                {
                    var updateInfoDoc = XDocument.Load(fileName);
                    XAttribute attr;
                    if ((attr = updateInfoDoc.Root.Attribute("binaries-etag")) != null)
                        retVal.BinariesETag = attr.Value;
                    DateTime lastChecked;
                    if ((attr = updateInfoDoc.Root.Attribute("last-check-timestamp")) != null)
                        if (DateTime.TryParseExact(attr.Value, "o", null,
                                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out lastChecked))
                            retVal.LastCheckTimestamp = lastChecked;
                    if ((attr = updateInfoDoc.Root.Attribute("last-check-error")) != null)
                        retVal.LastCheckError = attr.Value;
                }
                catch
                {
                }
            }
            return retVal;
        }

        public void Write(string fileName)
        {
            UpdateInfoFileContent updateInfoFileContent = this;
            var doc = new XDocument(new XElement("root"));
            if (updateInfoFileContent.BinariesETag != null)
                doc.Root.Add(new XAttribute("binaries-etag", updateInfoFileContent.BinariesETag));
            if (updateInfoFileContent.LastCheckTimestamp.HasValue)
                doc.Root.Add(new XAttribute("last-check-timestamp", updateInfoFileContent.LastCheckTimestamp.Value.ToString("o")));
            if (updateInfoFileContent.LastCheckError != null)
                doc.Root.Add(new XAttribute("last-check-error", updateInfoFileContent.LastCheckError));
            doc.Save(fileName);
        }
    };
}
