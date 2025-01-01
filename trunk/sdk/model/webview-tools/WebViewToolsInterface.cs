using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.WebViewTools
{
    /// <summary>
    /// Actions that can be performed with user-visible browser window.
    /// User input may be needed to pass by login screen.
    /// </summary>
    public interface IWebViewTools
    {
        Task<Stream> Download(DownloadParams downloadParams);
        Task<UploadFormResult> UploadForm(UploadFormParams uploadFormParams);
    };

    public enum CacheMode
    {
        AllowCacheReading,
        DisallowCacheReading,
        DownloadFromCacheOnly
    };

    public class DownloadParams
    {
        public Uri Location;
        public string ExpectedMimeType;
        public CancellationToken Cancellation;
        public Progress.IProgressAggregator Progress;
        public CacheMode CacheMode = CacheMode.AllowCacheReading;
        public Predicate<Stream> AllowCacheWriting;
        public Predicate<Uri> IsLoginUrl;
    };

    public class UploadFormParams
    {
        public Uri Location;
        public Uri FormUri;
        public CancellationToken Cancellation;
    };

    public class UploadFormResult
    {
        public IReadOnlyList<KeyValuePair<string, string>> Values;
    };
}
