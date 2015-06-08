using System;

namespace LogJoint.AppLaunch
{
	public interface IAppLaunch
	{
		bool TryParseLaunchUri(Uri uri, out LaunchUriData data);
	}

	public class LaunchUriData
	{
		public string SingleLogUri;
		public string WorkspaceUri;
	};
}
