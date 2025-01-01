using System;

namespace LogJoint.Preprocessing
{
    public interface ILogsDownloaderConfig
    {
        LogDownloaderRule GetLogDownloaderConfig(Uri forUri);
        void AddRule(Uri uri, LogDownloaderRule rule);
    };
}
