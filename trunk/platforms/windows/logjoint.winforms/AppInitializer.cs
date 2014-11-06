using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LogJoint
{
	class AppInitializer
	{
		public AppInitializer(LJTraceSource tracer)
		{
			InitializePlatform(tracer);
			InitLogFactories();
			UserDefinedFormatsManager.DefaultInstance.ReloadFactories();
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

		static void InitLogFactories()
		{
			Assembly[] asmsToAnalize = new Assembly[] { Assembly.GetEntryAssembly(), typeof(IModel).Assembly };

			foreach (Assembly asm in asmsToAnalize)
			{
				foreach (Type t in asm.GetTypes())
				{
					if (t.IsClass && typeof(ILogProviderFactory).IsAssignableFrom(t))
					{
						System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
					}
				}
			}
		}
	}
}
