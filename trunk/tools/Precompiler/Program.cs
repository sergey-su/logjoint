using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LogJoint;
using System.Xml;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;

namespace Precompiler
{
	class Program
	{
		public class FormatsRepositoryEntry : IFormatsRepositoryEntry
		{
			public FormatsRepositoryEntry(string location)
			{
				this.location = location;
			}

			public string Location { get { return location; } }
			public DateTime LastModified { get { return new DateTime(); } }
			public XElement LoadFormatDescription() { return XDocument.Load(location).Element("format");	}

			string location;
		};

		class FormatsRepository : IFormatsRepository
		{
			public void AddFormatDescription(string formatDescriptionFileName)
			{
				entries.Add(new FormatsRepositoryEntry(formatDescriptionFileName));
			}

			public IEnumerable<IFormatsRepositoryEntry> Entries	{ get {	return entries;	} }

			List<FormatsRepositoryEntry> entries = new List<FormatsRepositoryEntry>();
		};

		enum Platform
		{
			Windows,
			Silverlight
		};

		class CommandLineArgs
		{
			public CommandLineArgs(string[] args)
			{
				foreach (string arg in args)
				{
					Match m = switchRe.Match(arg);
					if (m.Success)
						HandleSwitch(m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim());
					else
						inputFiles.Add(arg);
				}
			}

			public IEnumerable<string> InputFiles { get { return inputFiles; } }
			public IEnumerable<string> References { get { return references; } }
			public Platform Platform { get { return platform; } }
			public string Output { get { return output; } }
			public bool DumpAsms { get { return dumpAsms; } }

			void HandleSwitch(string name, string value)
			{
				switch (name)
				{
					case "reference":
						references.Add(value);
						break;
					case "platform":
						if (value == "silverlight")
							platform = Platform.Silverlight;
						else
							Console.WriteLine("Warning: unknown platform '{0}'", value);
						break;
					case "output":
						output = value;
						break;
					case "dump-asms":
						dumpAsms = value == "yes";
						break;
					default:
						Console.WriteLine("Warning: unknown switch '{0}'", name);
						break;
				}
			}

			static Regex switchRe = new Regex(@"^\/([\w\-]+)\:(.+)$");

			List<string> inputFiles = new List<string>();
			List<string> references = new List<string>();
			Platform platform = Platform.Windows; // Platform of this tool is used by default
			string output = "";
			bool dumpAsms;
		};

		static void Main(string[] argsArray)
		{
			CommandLineArgs args = new CommandLineArgs(argsArray);

			Console.WriteLine("Precompiling for platform \"{0}\"", args.Platform);

			var referencedAsms = new Dictionary<string, Assembly>();

			foreach (string r in args.References)
			{
				Console.Write("Referencing assembly \"{0}\"", r);
				var asm = Assembly.ReflectionOnlyLoadFrom(r);
				referencedAsms.Add(asm.FullName, asm);
				Console.WriteLine("  OK");
			}

			var repo = new FormatsRepository();

			foreach (string f in args.InputFiles)
			{
				repo.AddFormatDescription(f);
			}

			var reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();

			XmlWriter output = null;
			if (args.Output != "")
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.Encoding = Encoding.UTF8;
				Console.WriteLine("Writing output package to \"{0}\"", args.Output);
				output = XmlTextWriter.Create(args.Output, settings);
				output.WriteStartDocument();
				output.WriteStartElement("formats");
			}

			foreach (var factory in formatsManager.Items)
			{
				string factoryName = factory.CompanyName + "/" + factory.FormatName;
				Console.WriteLine("Handling \"{1}\" from \"{0}\"", factory.Location, factoryName);
				Console.Write("      ");

				IUserCodePrecompile precompileIntf = factory as IUserCodePrecompile;
				if (precompileIntf == null)
				{
					Console.WriteLine("{0} doesn't support precompilation", factoryName);
					continue;
				}

				var userCodeType = precompileIntf.CompileUserCodeToType(CompilationTargetFx.Silverlight, 
					asmName => referencedAsms[asmName].Location);
				var userCodeAsm = userCodeType.Assembly;
				var asmFiles = userCodeAsm.GetFiles(true);
				if (asmFiles.Length != 1)
				{
					Console.WriteLine("Failed to precompile. More than one file in type's assembly");
					continue;
				}
				var asmFile = asmFiles[0];
				byte[] data = new byte[asmFile.Length];
				asmFile.Position = 0;
				asmFile.Read(data, 0, data.Length);
				string asmAsString = Convert.ToBase64String(data);

				XmlDocument fmtDoc = new XmlDocument();
				fmtDoc.Load(factory.Location);
				var fmtRoot = fmtDoc.SelectSingleNode("format");
				if (fmtRoot == null)
				{
					Console.WriteLine("Wrong file format");
					continue;
				}

				if (args.DumpAsms)
				{
					asmFile.Position = 0;
					using (var tmp = new FileStream(factory.FormatName + ".dll", FileMode.Create))
						asmFile.CopyTo(tmp);
				}

				var userCodeNode = fmtRoot.SelectSingleNode("precompiled-user-code");
				if (userCodeNode == null)
					userCodeNode = fmtRoot.AppendChild(fmtDoc.CreateElement("precompiled-user-code"));
				((XmlElement)userCodeNode).SetAttribute("platform", args.Platform.ToString().ToLower());
				((XmlElement)userCodeNode).SetAttribute("type", userCodeType.Name);
				userCodeNode.InnerText = asmAsString;

				if (output == null)
					fmtDoc.Save(factory.Location);
				else
					fmtDoc.Save(output);

				Console.WriteLine("Precompiled ok");
			}

			if (output != null)
			{
				output.WriteEndElement();
				output.WriteEndDocument();
				output.Close();
			}
			
			Console.WriteLine("All done");
		}
	}
}
