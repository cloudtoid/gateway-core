using System;
using static System.Threading.Timeout;

namespace Cloudtoid.GatewayCore.Settings
{
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
                internal const string ProxyName = Constants.ServerName;

                internal static class Upstream
                {
                    internal static class Request
                    {
                        internal static Version HttpVersion { get; } = Cloudtoid.HttpVersion.Version20;

                        internal static class Headers
                        {
                            internal const string CorrelationIdHeader = GatewayCore.Headers.Names.CorrelationId;
                        }

                        internal static class Sender
                        {
                            internal static TimeSpan Timeout { get; } = TimeSpan.FromMinutes(4);

                            internal static TimeSpan ConnectTimeout { get; } = InfiniteTimeSpan;

                            internal static TimeSpan Expect100ContinueTimeout { get; } = TimeSpan.FromSeconds(1);

                            internal static TimeSpan PooledConnectionIdleTimeout { get; } = TimeSpan.FromMinutes(2);

                            internal static TimeSpan PooledConnectionLifetime { get; } = InfiniteTimeSpan;

                            internal static TimeSpan ResponseDrainTimeout { get; } = TimeSpan.FromSeconds(2);

                            internal static int MaxAutomaticRedirections { get; } = 50;

                            internal static int MaxConnectionsPerServer { get; } = int.MaxValue;

                            internal static int MaxResponseDrainSizeInBytes { get; } = 1024 * 1024;

                            internal static int MaxResponseHeadersLengthInKilobytes { get; } = 64;
                        }
                    }
                }
            }
        }
    }
}