using System;
using System.Threading;

namespace LogJoint
{
    public static class IsBrowser
    {
        static Lazy<bool> value = new Lazy<bool>(() =>
        {
            var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            return os == "web" || os == "Browser";
        }, LazyThreadSafetyMode.PublicationOnly);

        public static bool Value => value.Value;
    }
}
