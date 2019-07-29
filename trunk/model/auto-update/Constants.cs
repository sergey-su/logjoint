using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Compression;
using LogJoint.Persistence;

namespace LogJoint.AutoUpdate
{
	static class Constants // todo: casing
	{
		public static readonly TimeSpan initialWorkerDelay = TimeSpan.FromSeconds(3);
		public static readonly TimeSpan checkPeriod = TimeSpan.FromHours(3);
		public static readonly string updateInfoFileName = "update-info.xml";
		public static readonly string updateLogKeyPrefix = "updatelog";

#if MONOMAC
		// on mac managed dlls are in logjoint.app/Contents/MonoBundle
		// Contents is the installation root. It is completely replaced during update.
		public static readonly string installationPathRootRelativeToManagedAssembliesLocation = "../";
		public static readonly string managedAssembliesLocationRelativeToInstallationRoot = "MonoBundle/";
		public static readonly string nativeExecutableLocationRelativeToInstallationRoot = "MacOS/logjoint";
#else
		// on win dlls are in root installation folder
		public static readonly string installationPathRootRelativeToManagedAssembliesLocation = ".";
		public static readonly string managedAssembliesLocationRelativeToInstallationRoot = ".";
		public static readonly string startAfterUpdateEventName = "LogJoint.Updater.StartAfterUpdate";
#endif
	};
}
