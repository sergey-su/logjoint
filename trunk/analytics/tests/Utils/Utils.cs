using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogJoint.Analytics
{
	static class Utils
	{
		public static Stream GetResourceStream(string nameSubstring)
		{
			var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(
				n => n.IndexOf(nameSubstring, StringComparison.InvariantCultureIgnoreCase) >= 0);
			Assert.IsNotNull(resourceName);
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
		}
	}
}
