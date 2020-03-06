namespace Cloudtoid.Foid.Config
{
    internal static class Constants
    {
        private static readonly string Section = "foid:";

        internal static class Proxy
        {
            private static readonly string Section = Constants.Section + "proxy:";

            internal static class Upstream
            {
                private static readonly string Section = Proxy.Section + "upstream:";

                internal static class Request
                {
                    private static readonly string Section = Upstream.Section + "request:";
                    internal static readonly string TimeoutInMilliseconds = Section + "timeoutInMilliseconds";

                    internal static class Headers
                    {
                        private static readonly string Section = Request.Section + "headers:";
                        internal static readonly string ProxyName = Section + "proxyName";
                        internal static readonly string DefaultHost = Section + "defaultHost";
                        internal static readonly string AllowHeadersWithEmptyValue = Section + "allowHeadersWithEmptyValue";
                        internal static readonly string AllowHeadersWithUnderscoreInName = Section + "allowHeadersWithUnderscoreInName";
                        internal static readonly string IncludeExternalAddress = Section + "includeExternalAddress";
                        internal static readonly string IgnoreClientAddress = Section + "ignoreClientAddress";
                        internal static readonly string IgnoreClientProtocol = Section + "ignoreClientProtocol";
                        internal static readonly string IgnoreRequestId = Section + "ignoreRequestId";
                        internal static readonly string IgnoreCallId = Section + "ignoreCallId";
                        internal static readonly string ExtraHeaders = Section + "extraHeaders";
                    }
                }
            }
        }
    }
}
