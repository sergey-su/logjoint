using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogJoint.Postprocessing
{
    public interface IUserNamesProvider
    {
        string ResolveObfuscatedUserName(string value);
    }

    public static class UsersNamingExtensions
    {
        public static string AddShortNameToUserHash(this IUserNamesProvider shortNames, string hash)
        {
            var resolvedName = shortNames.ResolveObfuscatedUserName(hash);
            if (resolvedName != null)
                return string.Format("{0} ({1})", resolvedName, hash);
            return hash;
        }

        public static string GetShortNameForUserHash(this IUserNamesProvider shortNames, string hash)
        {
            return shortNames.ResolveObfuscatedUserName(hash) ?? hash;
        }
    }
}