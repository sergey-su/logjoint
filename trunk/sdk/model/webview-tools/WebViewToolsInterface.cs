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
        public required Uri Location;
        public required string ExpectedMimeType;
        public CancellationToken Cancellation;
        public Progress.IProgressAggregator? Progress;
        public CacheMode CacheMode = CacheMode.AllowCacheReading;
        public Predicate<Stream>? AllowCacheWriting;
        public Predicate<Uri>? IsLoginUrl;
    };

    public class UploadFormParams
    {
        public required Uri Location;
        public required Uri FormUri;
        public CancellationToken Cancellation;
    };

    public class UploadFormResult(IReadOnlyList<KeyValuePair<string, string>> values)
    {
        public IReadOnlyList<KeyValuePair<string, string>> Values { get; private set; } = values;
    };
}
