using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using LogJoint;

namespace LogJoint.Writers
{
	public class NativeLogWriter: IJointLogWriter, IDisposable
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
			var type = msg.Flags & MessageFlag.TypeMask;
			switch (type)
			{
				case MessageFlag.StartFrame:
					writer.WriteStartElement("f");
					break;
				case MessageFlag.EndFrame:
					writer.WriteStartElement("ef");
					break;
				default:
					writer.WriteStartElement("m");
					break;
			}
			writer.WriteAttributeString("d", Listener.FormatDate(msg.Time.ToLocalDateTime()));
			writer.WriteAttributeString("t", msg.Thread.ID);
			if (type == MessageFlag.Content)
			{
				switch (msg.Flags & MessageFlag.ContentTypeMask)
				{
					case MessageFlag.Warning:
						writer.WriteAttributeString("s", "w");
						break;
					case MessageFlag.Error:
						writer.WriteAttributeString("s", "e");
						break;
				}
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
