using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
    public class PreprocessingManagerExtentionsRegistry : IExtensionsRegistry
    {
        public PreprocessingManagerExtentionsRegistry(ILogsDownloaderConfig logsDownloaderConfig)
        {
            this.logsDownloaderConfig = logsDownloaderConfig;
        }

        IEnumerable<IPreprocessingManagerExtension> IExtensionsRegistry.Items
        {
            get { return items; }
        }

        void IExtensionsRegistry.Register(IPreprocessingManagerExtension detector)
        {
            items.Add(detector);
        }

        void IExtensionsRegistry.AddLogDownloaderRule(Uri uri, LogDownloaderRule rule)
        {
            logsDownloaderConfig.AddRule(uri, rule);
        }

        readonly HashSet<IPreprocessingManagerExtension> items = new HashSet<IPreprocessingManagerExtension>();
        readonly ILogsDownloaderConfig logsDownloaderConfig;
    };
}
