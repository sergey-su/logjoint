using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Reflection;
using LogJoint;
using System.Xml.Linq;

namespace logjoint.web
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();
		}

		class SilverlightFormatsRepository : IFormatsRepository
		{
			public SilverlightFormatsRepository(Assembly repositoryAssembly)
			{
				XDocument doc = XDocument.Load(repositoryAssembly.GetManifestResourceStream("LogJoint.Formats.FormatsRepository.xml"));
				entries = new List<IFormatsRepositoryEntry>(doc.Element("formats").Elements("format").Select(
					fmtElt => new FormatsRepositoryEntry() { root = fmtElt }).Cast<IFormatsRepositoryEntry>());
			}

			public IEnumerable<IFormatsRepositoryEntry> Entries { get { return entries; } }

			class FormatsRepositoryEntry : IFormatsRepositoryEntry
			{
				public string Location { get { return root.GetHashCode().ToString(); }	}
				public DateTime LastModified { get { return new DateTime(); } }
				public XElement LoadFormatDescription()	{ return root; }
				internal XElement root;
			};

			List<IFormatsRepositoryEntry> entries;
		};

		IMediaBasedReaderFactory CreateFactory()
		{
			var repo = new SilverlightFormatsRepository(Assembly.GetExecutingAssembly());
			var reg = new LogProviderFactoryRegistry();
			var formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			var factory = reg.Find("Skype", "Deobfuscated corelib log");
			return factory as IMediaBasedReaderFactory;
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			string testLogStream = @"
13:22:26.352 T#519 Now: 15-04-2011 13:22:26
13:22:26.353 T#519 Backbone: added Router
13:22:26.353 T#519 BroadcastChannelManager: called registerWithBB
13:22:26.353 T#519 Backbone: added BroadcastChannelManager
13:22:26.354 T#519 HostCache: called registerWithBB 
				";

			var factory = CreateFactory();
			using (StringStreamMedia media = new StringStreamMedia())
			{
				media.SetData(testLogStream);

				using (LogSourceThreads threads = new LogSourceThreads())
				using (IPositionedMessagesReader reader = factory.CreateMessagesReader(threads, media))
				{
					reader.UpdateAvailableBounds(false);

					List<MessageBase> msgs = new List<MessageBase>();					

					using (var parser = reader.CreateParser(new CreateParserParams(reader.BeginPosition)))
					{
						for (; ; )
						{
							var msg = parser.ReadNext();
							if (msg == null)
								break;
							msgs.Add(msg);
							listBox1.Items.Add(msg.Text);
						}
					}

				}
			}

		}
	}
}
