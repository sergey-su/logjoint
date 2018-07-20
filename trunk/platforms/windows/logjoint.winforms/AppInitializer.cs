using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class AppInitializer
	{
		public AppInitializer(
			LJTraceSource tracer, 
			IUserDefinedFormatsManager userDefinedFormatsManager, 
			ILogProviderFactoryRegistry factoryRegistry,
			ITempFilesManager tempFiles
		)
		{
			InitializePlatform(tracer);
			InitLogFactories(userDefinedFormatsManager, factoryRegistry, tempFiles);
			userDefinedFormatsManager.ReloadFactories();
		}

		public void WireUpCommandLineHandler(
			LogJoint.UI.Presenters.MainForm.IPresenter mainFormPresenter,
			AppLaunch.ICommandLineHandler handler)
		{
			mainFormPresenter.Loaded += async (s, e) =>
			{
				string[] args = Environment.GetCommandLineArgs();
				if (args.Length > 1)
				{
					var evtArgs = new AppLaunch.CommandLineEventArgs()
					{
						ContinueExecution = true
					};
					await handler.HandleCommandLineArgs(args.Skip(1).ToArray(), evtArgs);
					if (!evtArgs.ContinueExecution)
					{
						mainFormPresenter.Close();
					}
				}
			};
		}

		static void InitializePlatform(LJTraceSource tracer)
		{
			Thread.CurrentThread.Name = "Main thread";

			AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
			{
				string msg = "Unhahdled domain exception occured";
				if (e.ExceptionObject is Exception)
					tracer.Error((Exception)e.ExceptionObject, msg);
				else
					tracer.Error("{0}: ({1}) {2}", msg, e.ExceptionObject.GetType(), e.ExceptionObject);
			};
		}

		static void InitLogFactories(
			IUserDefinedFormatsManager userDefinedFormatsManager, 
			ILogProviderFactoryRegistry factoryRegistry,
			ITempFilesManager tempFiles)
		{
			RegularGrammar.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
			XmlFormat.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
			JsonFormat.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
			factoryRegistry.Register(new DebugOutput.Factory());
			factoryRegistry.Register(new WindowsEventLog.Factory());
			factoryRegistry.Register(new PlainText.Factory(tempFiles));
			factoryRegistry.Register(new XmlFormat.NativeXMLFormatFactory(tempFiles));
		}
	}
}
