using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using log4net;

namespace SampleLoggingApp
{
	public class Log4NetListener : TraceListener
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(SampleLoggingApp.Program));

		public Log4NetListener()
		{
		}

		public override void Write(string message)
		{
			log.Info(message);
		}

		public override void WriteLine(string message)
		{
			log.Info(message + Environment.NewLine);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string msg)
		{
			switch (eventType)
			{
				case TraceEventType.Critical:
					log.Fatal(msg);
					break;
				case TraceEventType.Error:
					log.Error(msg);
					break;
				case TraceEventType.Warning:
					log.Warn(msg);
					break;
				default:
					log.Info(msg);
					break;
			}
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			TraceEvent(eventCache, source, eventType, id, string.Format(format, args ?? new object[] { }));
		}
	};


	public class NLogListener : TraceListener
	{
		private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

		public NLogListener()
		{
		}

		public override void Write(string message)
		{
			log.Info(message);
		}

		public override void WriteLine(string message)
		{
			log.Info(message + Environment.NewLine);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string msg)
		{
			switch (eventType)
			{
				case TraceEventType.Critical:
					log.Fatal(msg);
					break;
				case TraceEventType.Error:
					log.Error(msg);
					break;
				case TraceEventType.Warning:
					log.Warn(msg);
					break;
				default:
					log.Info(msg);
					break;
			}
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			TraceEvent(eventCache, source, eventType, id, string.Format(format, args ?? new object[] { }));
		}
	};


	public class Program
	{

		class Frame : IDisposable
		{
			TraceSource source;
			public Frame(TraceSource src, string frameName)
			{
				source = src;
				if (frameName == null)
				{
					System.Reflection.MethodBase m = (new StackFrame(2)).GetMethod();
					frameName = m.ToString();
				}
				src.TraceEvent(TraceEventType.Start, 0, frameName);
			}
			public Frame(TraceSource src): this(src, null)
			{
			}
			public void Dispose()
			{
				source.TraceEvent(TraceEventType.Stop, 0);
			}
		};

		static ManualResetEvent stopEvent;
		static TraceSource trace = new TraceSource("SampleApp");
		static Thread producerThread, consumerThread;
		static int timeoutBase = 1000;
		static bool addUnicodeMessages = false;


		static void Producer()
		{
			using (new Frame(trace))
			{
				trace.TraceInformation("----- Producer thread ------");

				Random rnd = new Random();
				for (; ; )
				{
					int timeout = timeoutBase + rnd.Next(2 * timeoutBase);

					if (stopEvent.WaitOne(timeout, true))
						break;

					string newFname = Guid.NewGuid().ToString() + ".data";

					using (new Frame(trace, "Processing new file: " + newFname))
					{
						string tempFileName = newFname + ".tmp";
						using (FileStream fs = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
						using (StreamWriter sw = new StreamWriter(fs))
						{
							int data = rnd.Next(timeoutBase);
							trace.TraceInformation("Data to be written: {0}", data);
							sw.Write(data.ToString());
						}
						File.Move(tempFileName, newFname);

						trace.TraceInformation("Data file was written OK");

						int timeout2 = rnd.Next(2 * timeoutBase);
						trace.TraceInformation("Waiting for a consumer to handle the file (timeout={0})", timeout2);

						Thread.Sleep(timeout2);

						try
						{
							using (FileStream fs = new FileStream(newFname, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100, FileOptions.DeleteOnClose))
							{
								trace.TraceEvent(TraceEventType.Warning, 0, "File was not handled by any consumer. Deleting it.");
							}
						}
						catch (FileNotFoundException)
						{
							trace.TraceInformation("File cannot be open which means that it was handled");
						}
						catch (IOException)
						{
							trace.TraceInformation("File cannot be open which means that it was handled");
						}
						catch (Exception e)
						{
							trace.TraceEvent(TraceEventType.Error, 0, e.Message);
						}
					}
				}
				trace.TraceInformation("Stop singnal received. Exiting.");
			}
		}

		static void Consumer()
		{
			using (new Frame(trace))
			{
				trace.TraceInformation("----- Consumer thread ------");

				Random rnd = new Random();
				for (; ; )
				{
					trace.TraceInformation("Searching for data files");

					bool fileConsumed = false;

					foreach (string f in Directory.GetFiles(".", "*.data"))
					{
						using (new Frame(trace, "Processing '" + f + "'..."))
						{
							FileStream fs = null;
							try
							{
								fs = new FileStream(f, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100, FileOptions.DeleteOnClose);
							}
							catch (FileNotFoundException)
							{
								trace.TraceInformation("Someone else has caught the file.");
							}
							catch (UnauthorizedAccessException)
							{
								trace.TraceInformation("Someone else has caught the file.");
							}
							catch (IOException)
							{
								trace.TraceInformation("File is locked.");
							}
							catch (Exception e)
							{
								trace.TraceEvent(TraceEventType.Error, 0, e.Message);
							}
							if (fs == null)
								continue;

							trace.TraceInformation("File is free. Consuming it and deleting.");

							using (fs)
							using (TextReader tr = new StreamReader(fs))
							{
								trace.TraceInformation("Message from the file: {0}", tr.ReadToEnd());
							}

							fileConsumed = true;

							break;
						}
					}

					if (!fileConsumed)
					{
						trace.TraceInformation("No free data file found. Going sleep.");
					}
					else
					{
						trace.TraceInformation("Relaxing after successful consuming.");
					}

					if (addUnicodeMessages)
					{
						using (new Frame(trace, "Unicode messages"))
						{
							trace.TraceInformation("Some symbols: {0}", "☺ℓ№℗℠™∏∑√∞∫≈≠≤‰ ♠♣♥♦♫☻");
							trace.TraceInformation("Persian string: {0}", "سلام جهان");
							trace.TraceInformation("Russian: {0}", "ПРИВЕТ");
							trace.TraceInformation("Greek: {0}", "γειά κόσμο");
							trace.TraceInformation("Chinese: {0}", "世界您好");
						}
					}

					if (stopEvent.WaitOne(rnd.Next(2 * timeoutBase), true))
						break;
				}

				trace.TraceInformation("Stop singnal received. Exiting.");
			}
		}

		static void Init()
		{
			using (new Frame(trace))
			{
				trace.TraceInformation("Setting domain-level exception handler");
				AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
				{
					trace.TraceEvent(TraceEventType.Error, 0, "Unhandled domain level exception: {0}", e.ExceptionObject);
				};

				trace.TraceInformation("Creating stop event");
				stopEvent = new ManualResetEvent(false);

				trace.TraceInformation("Starting producer thread");
				producerThread = new Thread(Producer);
				producerThread.Start();

				trace.TraceInformation("Starting consumer thread");
				consumerThread = new Thread(Consumer);
				consumerThread.Start();

				trace.TraceInformation("Initalization completed OK");
			}
		}

		static void Stop()
		{
			using (new Frame(trace))
			{
				trace.TraceInformation("Setting 'stop' event");
				stopEvent.Set();

				trace.TraceInformation("Waiting for the producer thread to finish...");
				producerThread.Join();
				trace.TraceInformation("Producer thread finished");

				trace.TraceInformation("Waiting for the consumer thread to finish...");
				consumerThread.Join();
				trace.TraceInformation("Consumer thread finished");
			}
		}


		static void ParseDensity(string str)
		{
			int val;
			if (int.TryParse(str, out val) && val > 0 && val <= 10)
			{
				timeoutBase = (new int[] {10000, 5000, 3000, 2000, 1000, 600, 400, 200, 100, 0})[val - 1];
				trace.TraceInformation("Setting timeoutBase to {0}", timeoutBase);
				return;
			}
			val = 5;
			trace.TraceEvent(TraceEventType.Warning, 0, "Wrong dentity argument: {0}. Defaulting to 5.", str);
			Console.WriteLine("Warning: wrong dentity argument. Defaulting to 5.");
		}

		static void ParseArgs(string[] args)
		{
			foreach (string arg in args)
			{
				string[] keyValuePair = arg.Split('=');
				if (keyValuePair.Length != 2)
					continue;
				switch (keyValuePair[0])
				{
					case "density":
						ParseDensity(keyValuePair[1]);
						break;
					case "unicode":
						addUnicodeMessages = keyValuePair[1] == "1";
						break;
				}
			}
		}

		public static void Main(string[] args)
		{
			var target = new NLog.Targets.MemoryTarget();
			var layout = new NLog.Layouts.CsvLayout();
			target.Layout = layout;
			//layout.Delimiter = NLog.Layouts.CsvColumnDelimiterMode
			//layout.Columns.Add(new NLog.Layouts.CsvColumn("",
			NLog.Layouts.CsvColumn col;
			//col.

			using (new Frame(trace))
			{
				trace.TraceInformation("----- Sample application started {0} ----", DateTime.Now);
				trace.TraceInformation("Command line arguments: {0}", string.Join(Environment.NewLine, args));

				ParseArgs(args);

				Init();

				string outputFmt = @"
LogJoint demonstration application started. Press any key to terminate.

This a sample multi-threaded application that starts two threads in addition to 
the main one. The first thread produces files, the second - consumes them and 
then deletes. The threads use random timeouts which makes the interaction 
between threads more interesting.

The application is configured to write trace information to the log file 
(debug.log) and to debug output. You can open the log with LogJoint by using 
Microsoft\TextWriterTraceListener source type. You can also listen to the 
debug output by adding Microsoft\OutputDebugString log source.

You can start several instances of SampleLoggingApp.exe. That will produce 
a little bit more intersting logs. Note that secondary logs have this format:
	<some guid>debug.log
for instance:
	681caf9d-b264-4ee3-a8e5-0d083ebc32f9debug.log

You can define the realative density of logs (messages per second) with parameter:
	SampleLoggingApp.exe density=<relative density 1-10>
for instance:
	SampleLoggingApp.exe density=3

Density 10 means ""no timeouts"". The logs will grow as quick as possible.

LogJoint supports utf-8, utf-16 and other possible text encodings. Specify parameter:
	SampleLoggingApp.exe unicode=1
to get the sample application to write non-ascii messages into the log. LogJoint should 
display those messages correctly.

Process information:
	Process ID           {0}
	Main thread ID       {1}
	Producer thread ID   {2}
	Consumer thread ID   {3}

Press any key to terminate the application.
";
				Console.Write(string.Format(outputFmt,
					Process.GetCurrentProcess().Id, 
					Thread.CurrentThread.ManagedThreadId,
					producerThread.ManagedThreadId,
					consumerThread.ManagedThreadId
				));

				Console.ReadKey();

				Stop();
			}
		}
	}
}
