using System;
using LogJoint.Persistence;
using LogJoint.Preprocessing;

namespace LogJoint.Wasm
{
    class BlazorWebContentConfig : IWebContentCacheConfig, ILogsDownloaderConfig
    {
        bool IWebContentCacheConfig.IsCachingForcedForHost(string hostName) => true;
        LogDownloaderRule ILogsDownloaderConfig.GetLogDownloaderConfig(Uri forUri) => null;
        void ILogsDownloaderConfig.AddRule(Uri uri, LogDownloaderRule rule) { }
    };
}
