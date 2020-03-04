namespace Cloudtoid.Foid
{
    internal static class ConfigConstants
    {
        private static readonly string FoidSection = "foid";

        internal static class Proxy
        {
            private static readonly string ProxySection = FoidSection + ":" + "proxy";

            internal static readonly string TotalTimeoutInMillisecondsSection = ProxySection + ":" + "TotalTimeoutInMilliseconds";
        }
    }
}
