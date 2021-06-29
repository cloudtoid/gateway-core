using System;
using System.Net.Http;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class UpstreamRequestSenderSettings
    {
        internal UpstreamRequestSenderSettings(
            string httpClientName,
            string? timeoutExpression,
            TimeSpan connectTimeout,
            TimeSpan expect100ContinueTimeout,
            TimeSpan pooledConnectionIdleTimeout,
            TimeSpan pooledConnectionLifetime,
            TimeSpan responseDrainTimeout,
            int maxAutomaticRedirections,
            int maxConnectionsPerServer,
            int maxResponseDrainSizeInBytes,
            int maxResponseHeadersLengthInKilobytes,
            bool allowAutoRedirect,
            bool useCookies)
        {
            HttpClientName = httpClientName;
            TimeoutExpression = timeoutExpression;
            ConnectTimeout = connectTimeout;
            Expect100ContinueTimeout = expect100ContinueTimeout;
            PooledConnectionIdleTimeout = pooledConnectionIdleTimeout;
            PooledConnectionLifetime = pooledConnectionLifetime;
            ResponseDrainTimeout = responseDrainTimeout;
            MaxAutomaticRedirections = maxAutomaticRedirections;
            MaxConnectionsPerServer = maxConnectionsPerServer;
            MaxResponseDrainSizeInBytes = maxResponseDrainSizeInBytes;
            MaxResponseHeadersLengthInKilobytes = maxResponseHeadersLengthInKilobytes;
            AllowAutoRedirect = allowAutoRedirect;
            UseCookies = useCookies;
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="HttpClient"/> to be used for upstream requests.
        /// Every time a route is defined in settings or an existing route is modified, we need to recreate the named
        /// <see cref="HttpClient"/>. The name here is used by the <see cref="IHttpClientFactory"/> to get instances of
        /// <see cref="HttpClient"/> configured with the request sender settings specified in <see cref="UpstreamRequestSenderSettings"/>.
        /// </summary>
        public string HttpClientName { get; }

        public string? TimeoutExpression { get; }

        public TimeSpan ConnectTimeout { get; }

        public TimeSpan Expect100ContinueTimeout { get; }

        public TimeSpan PooledConnectionIdleTimeout { get; }

        public TimeSpan PooledConnectionLifetime { get; }

        public TimeSpan ResponseDrainTimeout { get; }

        public int MaxAutomaticRedirections { get; }

        public int MaxConnectionsPerServer { get; }

        public int MaxResponseDrainSizeInBytes { get; }

        public int MaxResponseHeadersLengthInKilobytes { get; }

        public bool AllowAutoRedirect { get; }

        public bool UseCookies { get; }

        public TimeSpan EvaluateTimeout(ProxyContext context)
        {
            var result = context.Evaluate(TimeoutExpression);

            return long.TryParse(result, out var timeout) && timeout > 0
                ? TimeSpan.FromMilliseconds(timeout)
                : Defaults.Route.Proxy.Upstream.Request.Sender.Timeout;
        }
    }
}
