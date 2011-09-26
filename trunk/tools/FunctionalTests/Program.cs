using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace FunctionalTests
{
	interface ITest
	{
		string Name { get; }
		string ArgsHelp { get; }
		string Description { get; }
		void Run(string[] args);
	};

	class Program
	{
		List<ITest> tests = new List<ITest>();

		void CreateTests()
		{
			foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (t.IsClass && typeof(ITest).IsAssignableFrom(t))
				{
					tests.Add((ITest)Activator.CreateInstance(t));
				}
			}
		}

		void ShowUsage()
		{
			Console.WriteLine("Usage: FunctionalTests <test name> <params>");
			Console.WriteLine("Test names:");
			foreach (ITest t in tests)
			{
				Console.WriteLine("    - {0} {1}", t.Name, t.ArgsHelp);
				Console.WriteLine("      {0}", t.Description);
			}
		}

		ITest FundTestToRun(string name)
		{
			name = name.ToLower();
			foreach (ITest t in tests)
			{
				if (t.Name.ToLower() == name)
				{
					return t;
				}
			}
			return null;
		}

		void Main2(string[] args)
		{
			CreateTests();
			if (args.Length == 0)
			{
				ShowUsage();
				return;
			}
			ITest t = FundTestToRun(args[0]);
			if (t == null)
			{
				Console.WriteLine("Test not found: {0}", args[0]);
				ShowUsage();
				return;
			}
			try
			{
				List<string> testArgs = new List<string>();
				for (int i = 1; i < args.Length; ++i)
					testArgs.Add(args[i]);
				t.Run(testArgs.ToArray());
				Console.WriteLine("Test passed OK");
			}
			catch (Exception e)
			{
				Console.WriteLine("Test failed: {0}\n{1}", e.Message, e.StackTrace);
			}
		}

		static void Main(string[] args)
		{
			(new Program()).Main2(args);
		}
	}
}
