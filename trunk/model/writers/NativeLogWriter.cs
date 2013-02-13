using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using LogJoint;

namespace LogJoint.Writers
{
	public class NativeLogWriter: ILogWriter, IDisposable
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

		public void WriteMessage(MessageBase msg)
		{
			var type = msg.Flags & MessageBase.MessageFlag.TypeMask;
			switch (type)
			{
				case MessageBase.MessageFlag.StartFrame:
					writer.WriteStartElement("f");
					break;
				case MessageBase.MessageFlag.EndFrame:
					writer.WriteStartElement("ef");
					break;
				default:
					writer.WriteStartElement("m");
					break;
			}
			writer.WriteAttributeString("d", Listener.FormatDate(msg.Time.ToLocalDateTime()));
			writer.WriteAttributeString("t", msg.Thread.ID);
			if (type == MessageBase.MessageFlag.Content)
			{
				switch (msg.Flags & MessageBase.MessageFlag.ContentTypeMask)
				{
					case MessageBase.MessageFlag.Warning:
						writer.WriteAttributeString("s", "w");
						break;
					case MessageBase.MessageFlag.Error:
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
