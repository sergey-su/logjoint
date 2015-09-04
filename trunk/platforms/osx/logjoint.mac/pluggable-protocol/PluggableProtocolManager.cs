using LogJoint.AppLaunch;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class PluggableProtocolManager : IAppLaunch
	{
		public PluggableProtocolManager()
		{
		}

		bool IAppLaunch.TryParseLaunchUri(Uri uri, out LaunchUriData data)
		{
			data = null;
			return false;
		}
	}
}
