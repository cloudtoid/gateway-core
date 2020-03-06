namespace Cloudtoid.Foid
{
    internal static class ConfigConstants
    {
        private static readonly string Section = "foid";

        internal static class Proxy
        {
            private static readonly string Section = ConfigConstants.Section + ":" + "proxy";
            internal static readonly string TotalTimeoutInMilliseconds = Section + ":" + "totalTimeoutInMilliseconds";

            internal static class UpstreamRequest
            {
                private static readonly string Section = Proxy.Section + ":" + "upstreamRequest";

                internal static class Headers
                {
                    private static readonly string Section = UpstreamRequest.Section + ":" + "headers";
                    internal static readonly string ProxyName = Section + ":" + "proxyName";
                    internal static readonly string DefaultHost = Section + ":" + "defaultHost";
                    internal static readonly string AllowHeadersWithEmptyValue = Section + ":" + "allowHeadersWithEmptyValue";
                    internal static readonly string AllowHeadersWithUnderscoreInName = Section + ":" + "allowHeadersWithUnderscoreInName";
                    internal static readonly string IncludeExternalAddress = Section + ":" + "includeExternalAddress";
                    internal static readonly string IgnoreClientAddress = Section + ":" + "ignoreClientAddress";
                    internal static readonly string IgnoreClientProtocol = Section + ":" + "ignoreClientProtocol";
                    internal static readonly string IgnoreRequestId = Section + ":" + "ignoreRequestId";
                    internal static readonly string IgnoreCallId = Section + ":" + "ignoreCallId";
                    internal static readonly string ExtraHeaders = Section + ":" + "extraHeaders";

                    ////internal static class ExtraHeaders
                    ////{
                    ////    private static readonly string Section = Headers.Section + ":" + "extraHeaders";
                    ////    internal static readonly string Key = Section + ":" + "key";
                    ////    internal static readonly string Values = Section + ":" + "values";
                    ////}
                }
            }
        }
    }
}
