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
using System.Runtime.InteropServices;

namespace LogJoint.AutoUpdate
{
    static class Constants // todo: casing
    {
        public static readonly TimeSpan initialWorkerDelay = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan checkPeriod = TimeSpan.FromHours(3);
        public static readonly string updateInfoFileName = "update-info.xml";
        public static readonly string updateLogKeyPrefix = "updatelog";

        // on mac managed dlls are in logjoint.app/Contents/MonoBundle
        // Contents is the installation root. It is completely replaced during update.
        // on win dlls are in root installation folder
        public static readonly string installationPathRootRelativeToManagedAssembliesLocation =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "../" : ".";
        public static readonly string managedAssembliesLocationRelativeToInstallationRoot =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "MonoBundle/" : ".";
        public static readonly string? nativeExecutableLocationRelativeToInstallationRoot =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "MacOS/logjoint" : null;
        public static readonly string? startAfterUpdateEventName =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? null : "LogJoint.Updater.StartAfterUpdate";
    };
}
