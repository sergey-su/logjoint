using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class WebContentCache : IWebContentCache
	{
		readonly IContentCache rawContentCache;
		readonly IWebContentCacheConfig config;

		public WebContentCache(IContentCache rawContentCache, IWebContentCacheConfig config)
		{
			this.rawContentCache = rawContentCache;
			this.config = config;
		}

		Stream IWebContentCache.GetValue(Uri uri)
		{
			if (config.IsCachingForcedForHost(uri.Host.ToLower()))
				return rawContentCache.GetValue(MakeCacheKey(uri));
			return null;
		}

		async Task IWebContentCache.SetValue(Uri uri, Stream data)
		{
			if (config.IsCachingForcedForHost(uri.Host.ToLower()))
				await rawContentCache.SetValue(MakeCacheKey(uri), data);
		}

		static string MakeCacheKey(Uri uri)
		{
			return uri.ToString();
		}
	};
}
