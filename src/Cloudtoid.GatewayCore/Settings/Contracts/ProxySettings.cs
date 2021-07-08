namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class ProxySettings
    {
        internal ProxySettings(
            string to,
            string? proxyNameExpression,
            UpstreamRequestSettings upstreamRequest,
            DownstreamResponseSettings downstreamResponse)
        {
            To = to;
            ProxyNameExpression = proxyNameExpression;
            UpstreamRequest = upstreamRequest;
            DownstreamResponse = downstreamResponse;
        }

        public string To { get; }

        public string? ProxyNameExpression { get; }

        public UpstreamRequestSettings UpstreamRequest { get; }

        public DownstreamResponseSettings DownstreamResponse { get; }

        public string EvaluateProxyName(ProxyContext context)
        {
            var eval = context.Evaluate(ProxyNameExpression);

            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.ProxyName
                : eval;
        }
    }
}