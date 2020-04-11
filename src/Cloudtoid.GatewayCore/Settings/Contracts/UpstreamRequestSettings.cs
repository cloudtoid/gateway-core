namespace Cloudtoid.GatewayCore.Settings
{
    using System;

    public sealed class UpstreamRequestSettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? httpVersionExpression;

        internal UpstreamRequestSettings(
            RouteSettingsContext context,
            string? httpVersionExpression,
            UpstreamRequestHeadersSettings headers,
            UpstreamRequestSenderSettings sender)
        {
            this.context = context;
            this.httpVersionExpression = httpVersionExpression;
            Headers = headers;
            Sender = sender;
        }

        public UpstreamRequestHeadersSettings Headers { get; }

        public UpstreamRequestSenderSettings Sender { get; }

        public Version GetHttpVersion(ProxyContext proxyContext)
        {
            var result = context.Evaluate(proxyContext, httpVersionExpression);
            return HttpVersion.ParseOrDefault(result) ?? Defaults.Route.Proxy.Upstream.Request.HttpVersion;
        }
    }
}