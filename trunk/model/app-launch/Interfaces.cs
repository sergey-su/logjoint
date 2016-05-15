using System;

namespace LogJoint.AppLaunch
{
	public interface ICommandLineHandler
	{
		void HandleCommandLineArgs(string[] args);
	};

	public interface ILaunchUrlParser
	{
		bool TryParseLaunchUri(Uri uri, out LaunchUriData data);
		string ProtocolName { get; }
	}

	public class LaunchUriData
	{
		public string SingleLogUri;
		public string WorkspaceUri;
	};
}
