using LogJoint.Preprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.AppLaunch
{
	public class CommandLineHandler : ICommandLineHandler
	{
		readonly Preprocessing.IManager preprocessingManager;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly List<IBatchCommandHandler> commandHandlers = new List<IBatchCommandHandler>();

		public CommandLineHandler(
			Preprocessing.IManager preprocessingManager,
			IStepsFactory preprocessingStepsFactory)
		{
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		async Task ICommandLineHandler.HandleCommandLineArgs(string[] args, CommandLineEventArgs evtArgs)
		{
			args = await HandleBatchArg(args, evtArgs);
			args = RemoveNamedArgs(args).ToArray();
			OpenArgsAsLogSources(args);
		}

		void ICommandLineHandler.RegisterCommandHandler(IBatchCommandHandler handler)
		{
			commandHandlers.Add(handler);
		}

		private void OpenArgsAsLogSources(string[] args)
		{
			if (args.Length == 0)
				return;
			preprocessingManager.Preprocess(
				args.Select(arg => preprocessingStepsFactory.CreateLocationTypeDetectionStep(new PreprocessingStepParams(arg))),
				"Processing command line arguments"
			);
		}

		async private Task<string[]> HandleBatchArg(string[] args, CommandLineEventArgs evtArgs)
		{
			var argsCopy = (string[])args.Clone();

			var batchArgIdx = args.IndexOf(a => a.ToLower() == "/batch");
			if (batchArgIdx != null)
			{
				argsCopy[batchArgIdx.Value] = null;
				if (args.Length > batchArgIdx.Value + 1)
				{
					argsCopy[batchArgIdx.Value + 1] = null;
					var batchFile = BatchFile.TryLoad(args[batchArgIdx.Value + 1]);
					if (batchFile != null)
					{
						evtArgs.ContinueExecution = batchFile.StartUI;
						var tasks = new List<Task>();
						foreach (var cmd in batchFile.EnumCommands())
						{
							var handler = commandHandlers.FirstOrDefault(h => h.SupportedCommands.Contains(cmd.Key));
							if (handler != null)
								tasks.Add(Task.Run(() => handler.Run(cmd.Value)));
						}
						await Task.WhenAll(tasks);
					}
				}
			}

			return argsCopy.Where(a => a != null).ToArray();
		}

		IEnumerable<string> RemoveNamedArgs(string[] args)
		{

			bool nextIsNamedArg = false;
			foreach (var arg in args)
			{
				if (arg.StartsWith("--"))
					nextIsNamedArg = true;
				else if (nextIsNamedArg)
					nextIsNamedArg = false;
				else
					yield return arg;
			}
		}

		class BatchFile
		{
			XDocument doc;

			public static BatchFile TryLoad(string fileName)
			{
				var ret = new BatchFile();
				try
				{
					if (fileName == "-")
						using (var stdIn = Console.OpenStandardInput())
							ret.doc = XDocument.Load(stdIn);
					else
						ret.doc = XDocument.Load(fileName);
				}
				catch
				{
					return null;
				}
				return ret;
			}

			public IEnumerable<KeyValuePair<string, XElement>> EnumCommands()
			{
				foreach (var e in doc.Root.Elements("command"))
				{
					var type = e.AttributeValue("type");
					if (type != "")
						yield return new KeyValuePair<string, XElement>(type, e);
				}
			}

			public bool StartUI
			{
				get
				{
					var configVal = doc.Root.Element("startUi").SafeValue();
					return configVal == "true" || configVal == "1";
				}
			}
		};
	};
}
