using System;
using System.Threading;
using System.Reflection;
using System.Linq;

namespace LogJoint
{
	public class AppInitializer
	{
		public AppInitializer(
			LJTraceSource tracer, 
			IUserDefinedFormatsManager userDefinedFormatsManager, 
			ILogProviderFactoryRegistry factoryRegistry,
			ITempFilesManager tempFiles)
		{
			InitializePlatform(tracer);
			InitLogFactories(userDefinedFormatsManager, factoryRegistry, tempFiles);
			userDefinedFormatsManager.ReloadFactories();
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
			factoryRegistry.Register(new PlainText.Factory(tempFiles));
			factoryRegistry.Register(new XmlFormat.NativeXMLFormatFactory(tempFiles));
		}
	}
}

