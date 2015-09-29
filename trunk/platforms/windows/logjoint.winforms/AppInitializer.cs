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
		public AppInitializer(LJTraceSource tracer, IUserDefinedFormatsManager userDefinedFormatsManager, ILogProviderFactoryRegistry factoryRegistry, UI.ILogProviderUIsRegistry providerUIsRegistry)
		{
			InitializePlatform(tracer);
			InitLogFactories(tracer, userDefinedFormatsManager, factoryRegistry);
			userDefinedFormatsManager.ReloadFactories();
			RegisterLogProviderUIs(providerUIsRegistry);
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

		static void InitLogFactories(LJTraceSource tracer, IUserDefinedFormatsManager userDefinedFormatsManager, ILogProviderFactoryRegistry factoryRegistry)
		{
			using (tracer.NewFrame)
			{
				var asmsToAnalize = new Assembly[] {
					Assembly.GetEntryAssembly(),
					typeof(IModel).Assembly
				};
				var factoryTypes = asmsToAnalize.SelectMany(a => a.GetTypes())
					.Where(t => t.IsClass && typeof(ILogProviderFactory).IsAssignableFrom(t));

				foreach (Type t in factoryTypes)
				{
					tracer.Info("initing factory {0}", t.FullName);
					System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
					var registrationMethod = (
						from m in t.GetMethods(BindingFlags.Static | BindingFlags.Public)
						where m.GetCustomAttributes(typeof(RegistrationMethodAttribute), true).Length == 1
						let args = m.GetParameters()
						where args.Length == 1
						let isUserDefined = typeof(IUserDefinedFormatsManager) == args[0].ParameterType
						let isBuiltin = typeof(ILogProviderFactoryRegistry) == args[0].ParameterType
						where isUserDefined || isBuiltin
						select new { Method = m, Arg = isUserDefined ? (object)userDefinedFormatsManager : (object)factoryRegistry }
					).FirstOrDefault();
					if (registrationMethod != null)
					{
						t.InvokeMember(registrationMethod.Method.Name,
							BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
							null, null, new object[] { registrationMethod.Arg });
					}
				}
			}
		}

		void RegisterLogProviderUIs(UI.ILogProviderUIsRegistry providerUIsRegistry)
		{
			providerUIsRegistry.Register(StdProviderFactoryUIs.FileBasedProviderUIKey, f => new UI.FileLogFactoryUI((IFileBasedLogProviderFactory)f));
			providerUIsRegistry.Register(StdProviderFactoryUIs.DebugOutputProviderUIKey, f => new UI.DebugOutput.DebugOutputFactoryUI());
		}
	}
}
