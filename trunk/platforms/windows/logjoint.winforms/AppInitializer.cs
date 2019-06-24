using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	static class AppInitializer
	{
		public static void WireUpCommandLineHandler(
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
	}
}
