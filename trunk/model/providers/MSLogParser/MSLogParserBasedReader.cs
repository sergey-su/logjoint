using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using MSUtil;

namespace LogJoint.MSLogParser
{
	interface IMSLogParserLogReaderFactory : ILogReaderFactory
	{
		object CreateInputContext();
	};

	class MSLogParserBasedReader : MSLogParserXMLReader
	{
		public static IEnumerable<string> EnumSources(IConnectionParams p)
		{
			for (int i = 0; ; ++i)
			{
				string f = p["from" + i.ToString()];
				if (f == null)
					break;
				if (f == "")
					continue;
				yield return f;
			}
		}

		public static string GetUserFriendlyConnectionName(IConnectionParams connectParams, string formatBaseName)
		{
			StringBuilder ret = new StringBuilder();
			ret.Append(formatBaseName);
			int idx = 0;
			foreach (string f in EnumSources(connectParams))
			{
				ret.AppendFormat("{0}{1}", idx == 0 ? ": " : ", ", f);
				idx++;
			}
			if (connectParams["where"] != null)
				ret.Append(" (+additional conditions)");
			return ret.ToString();
		}

		static void CreateTempFile(string fname)
		{
			using (StreamWriter w = new StreamWriter(File.Create(fname), Encoding.UTF8))
			{
				w.Write("<ROOT></ROOT>");
			}
		}

		static string CreateTempFile(ILogReaderHost host)
		{
			string fname = host.TempFilesManager.GenerateNewName();
			CreateTempFile(fname);
			return fname;
		}

		public MSLogParserBasedReader(
			ILogReaderHost host,
			IMSLogParserLogReaderFactory factory,
			IConnectionParams p, 
			FieldsProcessor fieldsMapping) :

			base(host, factory, CreateTempFile(host), fieldsMapping)
		{
			stats.ConnectionParams.Assign(p);

			StringBuilder from = new StringBuilder();
			foreach (string f in EnumSources(p))
			{
				string quote = "";
				if (f.IndexOfAny(new char[] { ' ' }) >= 0)
					quote = "'";
				from.AppendFormat("{0}{2}{1}{2}", from.Length == 0 ? "" : ", ", f, quote);
			}
			this.from = from.ToString();

			this.where = p["where"];

			StartAsyncReader(p.ToString());
		}

		public override void Dispose()
		{
			bool disposed = IsDisposed;
			base.Dispose();
			if (!disposed)
				if (File.Exists(FileName))
					File.Delete(FileName);
		}

		protected override AsyncLogReader.Algorithm CreateAlgorithm()
		{
			return new LogReaderAlgorithm(this);
		}


		class LogReaderAlgorithm : RangeManagingAlgorithm
		{
			public LogReaderAlgorithm(MSLogParserBasedReader reader)
				: base(reader)
			{
				this.reader = reader;
			}

			static Regex timeCodePattern1 = new Regex(@"^TO_DATETIME\(\s*(\w+)\s*(\,\s*DEFAULT_DATETIME_FORMAT\(\s*\)\s*)?\)$");

			static string GetTimeSQLExpression(string code)
			{
				Match m;
				if ((m = timeCodePattern1.Match(code)).Success)
				{
					return m.Groups[1].Value;
				}
				throw new Exception("The expression for 'Time' field is not compatible with LogParser reader. Expression='" + code + "'");
			}

			protected override bool UpdateAvailableTime(bool incrementalMode)
			{
				if (incrementalMode)
					return false;

				ILogQuery q = new LogQueryClassClass();

				ICOMXMLOutputContext output = new COMXMLOutputContextClassClass();
				output.oCodepage = 65001; // utf8
				output.structure = 3; // write an element with name rowName for fields, put actual field name into attribute NAME
				output.schemaType = 0; // no schema
				output.rowName = "R"; // name of the element containing output record
				output.fieldName = "F";  // name of the element containing output field value

				StringBuilder query = new StringBuilder();

				query.Append("SELECT * ");

				query.AppendFormat(" INTO '{0}'", reader.FileName);
				query.AppendFormat(" FROM {0}", reader.from);
				query.AppendFormat(" ORDER BY {0}", GetTimeSQLExpression(reader.fieldsMapping.GetTimeFieldCode()));

				bool ret = q.ExecuteBatch(query.ToString(), reader.GetInputContext(), output);

				// LogParser deletes the output file if
				// there are no records to output. 
				// That looks like a bug. 
				// Here is a workaround: recreate the file if 
				// it has been deleted.
				if (!File.Exists(reader.FileName))
				{
					CreateTempFile(reader.FileName);
				}

				return base.UpdateAvailableTime(incrementalMode);
			}

			MSLogParserBasedReader reader;
		};

		internal object GetInputContext()
		{
			if (inputContext == null)
				inputContext = ((IMSLogParserLogReaderFactory)this.factory).CreateInputContext();
			return inputContext;
		}

		public readonly string from;
		public readonly string where;
		public object inputContext;
	}

	public class UserDefinedFormatFactory : UserDefinedFormatsManager.UserDefinedFactoryBase,
		IFileReaderFactory, IMSLogParserLogReaderFactory
	{
		List<string> patterns = new List<string>();
		FieldsProcessor fieldsMapping;
		Type inputContextType;
		string knownInputName;
		Dictionary<string, string> inputContextParams = new Dictionary<string,string>();

		static Dictionary<string, Type> knownLogparserInputs = new Dictionary<string,Type>();

		static UserDefinedFormatFactory()
		{
			try
			{
				new COMEventLogInputContextClass();
			}
			catch (COMException)
			{
				return;
			}

			UserDefinedFormatsManager.Instance.RegisterFormatType(
				"logparser", typeof(UserDefinedFormatFactory));

			knownLogparserInputs.Add("ADS", typeof(MSUtil.COMADSInputContextClassClass));
			knownLogparserInputs.Add("BIN", typeof(MSUtil.COMIISBINInputContextClassClass));
			knownLogparserInputs.Add("CSV", typeof(MSUtil.COMCSVInputContextClassClass));
			knownLogparserInputs.Add("ETW", typeof(MSUtil.COMETWInputContextClassClass));
			knownLogparserInputs.Add("EVT", typeof(MSUtil.COMEventLogInputContextClassClass));
			knownLogparserInputs.Add("FS", typeof(MSUtil.COMFileSystemInputContextClassClass));
			knownLogparserInputs.Add("HTTPERR", typeof(MSUtil.COMHttpErrorInputContextClassClass));
			knownLogparserInputs.Add("IIS", typeof(MSUtil.COMIISIISInputContextClassClass));
			knownLogparserInputs.Add("IISODBC", typeof(MSUtil.COMIISODBCInputContextClassClass));
			knownLogparserInputs.Add("IISW3C", typeof(MSUtil.COMIISW3CInputContextClassClass));
			knownLogparserInputs.Add("NCSA", typeof(MSUtil.COMIISNCSAInputContextClassClass));
			knownLogparserInputs.Add("NETMON", typeof(MSUtil.COMNetMonInputContextClassClass));
			knownLogparserInputs.Add("REG", typeof(MSUtil.COMRegistryInputContextClassClass));
			knownLogparserInputs.Add("TEXTLINE", typeof(MSUtil.COMTextLineInputContextClassClass));
			knownLogparserInputs.Add("TEXTWORD", typeof(MSUtil.COMTextWordInputContextClassClass));
			knownLogparserInputs.Add("TSV", typeof(MSUtil.COMTSVInputContextClassClass));
			knownLogparserInputs.Add("URLSCAN", typeof(MSUtil.COMURLScanLogInputContextClassClass));
			knownLogparserInputs.Add("W3C", typeof(MSUtil.COMW3CInputContextClassClass));
			knownLogparserInputs.Add("XML", typeof(MSUtil.COMXMLInputContextClassClass));
		}

		public UserDefinedFormatFactory(string fileName, XmlNode rootNode, XmlNode formatSpecificNode): 
			base(fileName, rootNode, formatSpecificNode)
		{
			foreach (XmlNode n in formatSpecificNode.SelectNodes("patterns/pattern[text()!='']"))
				patterns.Add(n.InnerText);

			fieldsMapping = new FieldsProcessor(formatSpecificNode.SelectSingleNode("fields-config") as XmlElement, true);

			XmlElement inputNode = (XmlElement)formatSpecificNode.SelectSingleNode("input");
			string guid = inputNode.GetAttribute("guid");
			if (!string.IsNullOrEmpty(guid))
			{
				inputContextType = Type.GetTypeFromCLSID(new Guid(guid));
			}
			else
			{
				string inputName = inputNode.GetAttribute("name");
				if (!knownLogparserInputs.TryGetValue(inputName, out inputContextType))
				{
					throw new Exception("Invalid input context name: '" + inputName + "'");
				}
				knownInputName = inputName;
			}
			if (inputContextType == null)
				throw new Exception("LogParser input type is not defined");

			foreach (XmlElement p in inputNode.SelectNodes("param"))
			{
				inputContextParams[p.GetAttribute("name")] = p.InnerText;
			}
		}

		public string KnownInputName { get { return knownInputName; } }

			#region IMSLogParserLogReaderFactory Members

			public object CreateInputContext()
			{
				object inputCtx = Activator.CreateInstance(inputContextType);
				foreach (KeyValuePair<string, string> p in inputContextParams)
				{
					System.Reflection.PropertyInfo pi = inputContextType.GetProperty(p.Key);
					if (pi == null)
						throw new Exception("Wrong input param: " + p.Key);
					inputContextType.InvokeMember(p.Key, System.Reflection.BindingFlags.SetProperty,
						null, inputCtx, new object[] { Convert.ChangeType(p.Value, pi.PropertyType) });
				}
				return inputCtx;
			}

			#endregion

			#region ILogReaderFactory Members

			public  override ILogReaderFactoryUI CreateUI()
			{
				return new LogParsedBaseFactoryUI(this);
			}

			public override string GetUserFriendlyConnectionName(IConnectionParams connectParams)
			{
				return MSLogParserBasedReader.GetUserFriendlyConnectionName(connectParams, this.knownInputName);
			}

			public override ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams)
			{
				return new MSLogParserBasedReader(host, this, connectParams, new FieldsProcessor(fieldsMapping));
			}

			#endregion

		#region IFileReaderFactory Members

		public IEnumerable<string> SupportedPatterns
		{
			get
			{
				return patterns;
			}
		}

		public IConnectionParams CreateParams(string fileName)
		{
			ConnectionParams p = new ConnectionParams();
			p["from0"] = fileName;
			return p;
		}

		#endregion
	};
}
