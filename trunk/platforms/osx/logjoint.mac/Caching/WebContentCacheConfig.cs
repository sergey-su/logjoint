using LogJoint.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
	class WebContentCacheConfig : IWebContentCacheConfig
	{
		readonly Lazy<HashSet<string>> forcedCachingFor;

		public WebContentCacheConfig()
		{
			forcedCachingFor = new Lazy<HashSet<string>>(() =>
			{
					return new HashSet<string>(new [] {"devlogs.skype.net"});
			}, true);
		}

		bool IWebContentCacheConfig.IsCachingForcedForHost(string hostName)
		{
			return forcedCachingFor.Value.Contains(hostName);
		}
	}
}
