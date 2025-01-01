using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using LogJoint;

namespace LogJoint.Writers
{
    public class NativeLogWriter : IDisposable
    {
        public NativeLogWriter(Stream output)
        {
            var writerSettings = new XmlWriterSettings();
            writerSettings.CloseOutput = false;
            writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            writerSettings.Indent = true;
            writerSettings.IndentChars = "  ";
            writerSettings.Encoding = Encoding.UTF8;
            writerSettings.OmitXmlDeclaration = true;

            this.writer = XmlWriter.Create(output, writerSettings);
        }

        public void WriteMessage(IMessage msg)
        {
            writer.WriteStartElement("m");
            writer.WriteAttributeString("d", Listener.FormatDate(msg.Time.ToLocalDateTime()));
            writer.WriteAttributeString("t", msg.Thread.ID);
            switch (msg.Severity)
            {
                case SeverityFlag.Warning:
                    writer.WriteAttributeString("s", "w");
                    break;
                case SeverityFlag.Error:
                    writer.WriteAttributeString("s", "e");
                    break;
            }
            writer.WriteString(msg.Text.Value);
            writer.WriteEndElement();
        }

        public void Dispose()
        {
            writer.Close();
        }

        readonly XmlWriter writer;
    }
}
