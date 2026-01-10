using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LogJoint.AppLaunch
{
    public class LaunchUrlParser : ILaunchUrlParser
    {
        bool ILaunchUrlParser.TryParseLaunchUri(Uri uri, [MaybeNullWhen(false)] out LaunchUriData data)
        {
            data = null;
            if (string.Compare(uri.Scheme, protocolName, true) != 0)
                return false;
            data = CreateData(uri);
            return data != null;
        }

        string ILaunchUrlParser.ProtocolName { get { return protocolName; } }

        LaunchUriData? CreateData(Uri uri)
        {
            // Logic below involves parsing of query string.
            // Having this in a separate function ensures loading of System.Web.dll on demand 
            // only when pluggable protocol is used.

#if MONOMAC
			var urlComponents = new Foundation.NSUrlComponents (
				new Foundation.NSUrl(uri.Query), resolveAgainstBaseUrl: false);
			var contentUri = urlComponents.QueryItems.FirstOrDefault (i => i.Name == "uri")?.Value;
			var t = urlComponents.QueryItems.FirstOrDefault (i => i.Name == "t")?.Value;
#else
            var args = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var contentUri = args.Get("uri");
            var t = args.Get("t");
#endif
            if (contentUri == null)
                return null;

            switch ((t ?? "").ToLower())
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
