using LogJoint.Drawing;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	static class CustomFormatPageUtils
	{
		public static string GetParameterStatusString(bool statusOk)
		{
			return statusOk ? "OK" : "Not set";
		}
		public static string GetTestPassedStatusString(bool passed)
		{
			return passed ? "Passed" : "";
		}
		public static Color GetLabelColor(bool statusOk)
		{
			return statusOk ? new Color(0xFF008000) : new Color(0xFF000000);
		}

		public static string GetFormatFileNameBasis(IUserDefinedFactory factory)
		{
			string fname = System.IO.Path.GetFileName(factory.Location);

			string suffix = ".format.xml";

			if (fname.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase))
				fname = fname.Remove(fname.Length - suffix.Length);

			return fname;
		}

		static void ChangeEncodingToUnicode(UserDefinedFactoryParams createParams)
		{
			var encodingNode = createParams.FormatSpecificNode.Element("encoding");
			if (encodingNode == null)
				createParams.FormatSpecificNode.Add(encodingNode = new XElement("encoding"));
			encodingNode.Value = "UTF-16";
		}

		public class TestParsing: ITestParsing
		{
			readonly IAlertPopup alerts;
			readonly ITempFilesManager tempFilesManager;
			readonly ITraceSourceFactory traceSourceFactory;
			readonly RegularExpressions.IRegexFactory regexFactory;
			readonly FieldsProcessor.IFactory fieldsProcessorFactory;
			readonly IFactory objectsFactory;

			public TestParsing(
				IAlertPopup alerts,
				ITempFilesManager tempFilesManager,
				ITraceSourceFactory traceSourceFactory,
				RegularExpressions.IRegexFactory regexFactory,
				FieldsProcessor.IFactory fieldsProcessorFactory,
				IFactory objectsFactory
			)
			{
				this.alerts = alerts;
				this.tempFilesManager = tempFilesManager;
				this.traceSourceFactory = traceSourceFactory;
				this.regexFactory = regexFactory;
				this.objectsFactory = objectsFactory;
				this.fieldsProcessorFactory = fieldsProcessorFactory;
			}

			bool? ITestParsing.Test(
				string sampleLog,
				XmlNode formatRoot,
				string formatSpecificNodeName
			)
			{
				if (sampleLog == "")
				{
					alerts.ShowPopup("", "Provide sample log first", AlertFlags.Ok | AlertFlags.WarningIcon);
					return null;
				}

				string tmpLog = tempFilesManager.GenerateNewName();
				try
				{
					XDocument clonedFormatXmlDocument = XDocument.Parse(formatRoot.OuterXml);

					UserDefinedFactoryParams createParams;
					createParams.Location = null;
					createParams.RootNode = clonedFormatXmlDocument.Element("format");
					createParams.FormatSpecificNode = createParams.RootNode.Element(formatSpecificNodeName);
					createParams.FactoryRegistry = null;
					createParams.RegexFactory = regexFactory;
					createParams.FieldsProcessorFactory = fieldsProcessorFactory;

					// Temporary sample file is always written in Unicode wo BOM: we don't test encoding detection,
					// we test regexps correctness.
					using (var w = new StreamWriter(tmpLog, false, new UnicodeEncoding(false, false)))
						w.Write(sampleLog);
					ChangeEncodingToUnicode(createParams);

					var cp = ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(tmpLog);

					ILogProviderFactory f;
					if (formatSpecificNodeName == RegularGrammar.UserDefinedFormatFactory.ConfigNodeName)
						f = new RegularGrammar.UserDefinedFormatFactory(createParams, tempFilesManager, traceSourceFactory);
					else if (formatSpecificNodeName == XmlFormat.UserDefinedFormatFactory.ConfigNodeName)
						f = new XmlFormat.UserDefinedFormatFactory(createParams, tempFilesManager, traceSourceFactory);
					else if (formatSpecificNodeName == JsonFormat.UserDefinedFormatFactory.ConfigNodeName)
						f = new JsonFormat.UserDefinedFormatFactory(createParams, tempFilesManager, traceSourceFactory);
					else
						return null;
					using (f as IDisposable)
					using (var interaction = objectsFactory.CreateTestDialog())
					{
						return interaction.ShowDialog(f, cp);
					}
				}
				finally
				{
					File.Delete(tmpLog);
				}
			}
		}
	};

	public interface ITestParsing
	{
		bool? Test(string sampleLog, XmlNode formatRoot, string formatSpecificNodeName);
	};
};