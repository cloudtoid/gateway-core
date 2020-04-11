namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class ProxySettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? correlationIdHeaderExpression;

        internal ProxySettings(
            RouteSettingsContext context,
            string to,
            string? correlationIdHeaderExpression,
            UpstreamRequestSettings upstreamRequest,
            DownstreamResponseSettings downstreamResponse)
        {
            this.context = context;
            To = to;
            this.correlationIdHeaderExpression = correlationIdHeaderExpression;
            UpstreamRequest = upstreamRequest;
            DownstreamResponse = downstreamResponse;
        }

        public string To { get; }

        public UpstreamRequestSettings UpstreamRequest { get; }

        public DownstreamResponseSettings DownstreamResponse { get; }

        public string GetCorrelationIdHeader(ProxyContext proxyContext)
        {
            var eval = context.Evaluate(proxyContext, correlationIdHeaderExpression);

            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                : eval;
        }
    }
}