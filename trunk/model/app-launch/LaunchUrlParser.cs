using System;

namespace LogJoint.AppLaunch
{
	public class LaunchUrlParser: ILaunchUrlParser
	{
		bool ILaunchUrlParser.TryParseLaunchUri(Uri uri, out LaunchUriData data)
		{
			data = null;
			if (string.Compare(uri.Scheme, protocolName, true) != 0)
				return false;
			data = CreateData(uri);
			return data != null;
		}

		LaunchUriData CreateData(Uri uri)
		{
			// Logic below involves parsing of query string.
			// Having this in a separate function ensures loading of System.Web.dll on demand 
			// only when plauggable protocol is used.

			var args = System.Web.HttpUtility.ParseQueryString(uri.Query);
			var contentUri = args.Get("uri");
			if (contentUri == null)
				return null;

			switch ((args.Get("t") ?? "").ToLower())
			{
			case "log":
				return new LaunchUriData() { SingleLogUri = contentUri };
			case "workspace":
				return new LaunchUriData() { WorkspaceUri = contentUri };
			}

			return null;
		}

		const string protocolName = "logjoint";
	}
}
