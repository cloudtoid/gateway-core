namespace Cloudtoid.GatewayCore.Settings
{
    using System;

    internal static class Defaults
    {
        internal static class System
        {
            internal static int RouteCacheMaxCount { get; } = 100000;
        }

        internal static class Route
        {
            internal static class Proxy
            {
                internal static class Upstream
                {
                    internal static class Request
                    {
                        internal static Version HttpVersion { get; } = Cloudtoid.HttpVersion.Version20;

                        internal static class Headers
                        {
                            internal const string CorrelationIdHeader = GatewayCore.Headers.Names.CorrelationId;

                            internal const string ProxyName = "gwcore";

                            internal static string Host { get; } = Environment.MachineName;
                        }

                        internal static class Sender
                        {
                            internal static TimeSpan Timeout { get; } = TimeSpan.FromMinutes(4);
                        }
                    }
                }
            }
        }
    }
}