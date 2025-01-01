using LogJoint.Persistence;
using LogJoint.Preprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace LogJoint.Properties
{
    class WebContentConfig : IWebContentCacheConfig, ILogsDownloaderConfig
    {
        readonly Lazy<HashSet<string>> forcedCachingFor;
        readonly Lazy<Dictionary<string, LogDownloaderRule>> logDownloaderRules;

        public WebContentConfig()
        {
            forcedCachingFor = new Lazy<HashSet<string>>(() =>
            {
                return new HashSet<string>(
                    (Settings.Default.ForceWebContentCachingFor ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }, true);
            logDownloaderRules = new Lazy<Dictionary<string, LogDownloaderRule>>(() =>
            {
                var serializer = new DataContractJsonSerializer(typeof(LogDownloaderConfigDTO));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(Settings.Default.LogDownloaderConfig) ? "{} " : Settings.Default.LogDownloaderConfig)))
                {
                    var dto = (LogDownloaderConfigDTO)serializer.ReadObject(stream);
                    return (dto.Rules ?? new LogDownloaderConfigDTO.RuleDTO[0]).ToDictionary(x => x.Url, x => new LogDownloaderRule(
                        x.UseWebBrowserDownloader,
                        x.LoginUrls ?? new string[0],
                        x.ExpectedMimeType
                    ));
                }
            }, true);
        }

        LogDownloaderRule ILogsDownloaderConfig.GetLogDownloaderConfig(Uri forUri)
        {
            var uriPath = forUri.GetLeftPart(UriPartial.Path);
            return logDownloaderRules.Value.Where(rule => uriPath.Contains(rule.Key)).Select(rule => rule.Value).FirstOrDefault();
        }

        bool IWebContentCacheConfig.IsCachingForcedForHost(string hostName)
        {
            return forcedCachingFor.Value.Contains(hostName);
        }

        void ILogsDownloaderConfig.AddRule(Uri uri, LogDownloaderRule rule)
        {
            var uriPath = uri.GetLeftPart(UriPartial.Path);
            logDownloaderRules.Value.Add(uriPath, rule);
        }

        [DataContract]
        public class LogDownloaderConfigDTO
        {
            [DataContract]
            public class RuleDTO
            {
                [DataMember(Name = "url")]
                public string Url { get; protected set; }
                [DataMember(Name = "useWebBrowserDownloader")]
                public bool UseWebBrowserDownloader { get; protected set; }
                [DataMember(Name = "expectedMimeType")]
                public string ExpectedMimeType { get; protected set; }
                [DataMember(Name = "loginUrls")]
                public string[] LoginUrls { get; protected set; }
            };
            [DataMember(Name = "rules")]
            public RuleDTO[] Rules { get; protected set; }
        }
    }
}
