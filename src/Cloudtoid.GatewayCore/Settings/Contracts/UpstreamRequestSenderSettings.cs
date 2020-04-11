namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Net.Http;

    public sealed class UpstreamRequestSenderSettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? timeoutInMillisecondsExpression;

        internal UpstreamRequestSenderSettings(
            RouteSettingsContext context,
            string httpClientName,
            string? timeoutInMillisecondsExpression,
            bool allowAutoRedirect,
            bool useCookies)
        {
            this.context = context;
            HttpClientName = httpClientName;
            this.timeoutInMillisecondsExpression = timeoutInMillisecondsExpression;
            AllowAutoRedirect = allowAutoRedirect;
            UseCookies = useCookies;
        }

        /// <summary>
        /// Gets or sets the name of the <see cref="HttpClient"/> to be used for upstream requests.
        /// Every time a route is defined in settings or an existing route is modified, we need to recreate the
        /// named <see cref="HttpClient"/>. The name here is used by the <see cref="IHttpClientFactory"/> to get instances of
        /// <see cref="HttpClient"/> configured with the request sender settings specified in <see cref="UpstreamRequestSenderSettings"/>.
        /// </summary>
        internal string HttpClientName { get; }

        public bool AllowAutoRedirect { get; }

        public bool UseCookies { get; }

        public TimeSpan GetTimeout(ProxyContext proxyContext)
        {
            var result = context.Evaluate(proxyContext, timeoutInMillisecondsExpression);

            return long.TryParse(result, out var timeout) && timeout > 0
                ? TimeSpan.FromMilliseconds(timeout)
                : Defaults.Route.Proxy.Upstream.Request.Sender.Timeout;
        }
    }
}
