namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class ProxySettings
    {
        internal ProxySettings(
            string to,
            string? proxyNameExpression,
            string? correlationIdHeaderExpression,
            UpstreamRequestSettings upstreamRequest,
            DownstreamResponseSettings downstreamResponse)
        {
            To = to;
            ProxyNameExpression = proxyNameExpression;
            CorrelationIdHeaderExpression = correlationIdHeaderExpression;
            UpstreamRequest = upstreamRequest;
            DownstreamResponse = downstreamResponse;
        }

        public string To { get; }

        public string? ProxyNameExpression { get; }

        public string? CorrelationIdHeaderExpression { get; }

        public UpstreamRequestSettings UpstreamRequest { get; }

        public DownstreamResponseSettings DownstreamResponse { get; }

        public string EvaluateProxyName(ProxyContext context)
        {
            var eval = context.Evaluate(ProxyNameExpression);

            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.ProxyName
                : eval;
        }

        public string EvaluateCorrelationIdHeader(ProxyContext context)
        {
            var eval = context.Evaluate(CorrelationIdHeaderExpression);

            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                : eval;
        }
    }
}