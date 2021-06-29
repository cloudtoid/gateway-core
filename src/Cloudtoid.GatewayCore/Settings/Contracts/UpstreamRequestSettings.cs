using System;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class UpstreamRequestSettings
    {
        internal UpstreamRequestSettings(
            string? httpVersionExpression,
            UpstreamRequestHeadersSettings headers,
            UpstreamRequestSenderSettings sender)
        {
            HttpVersionExpression = httpVersionExpression;
            Headers = headers;
            Sender = sender;
        }

        public string? HttpVersionExpression { get; }

        public UpstreamRequestHeadersSettings Headers { get; }

        public UpstreamRequestSenderSettings Sender { get; }

        public Version EvaluateHttpVersion(ProxyContext context)
        {
            var result = context.Evaluate(HttpVersionExpression);
            return HttpVersion.ParseOrDefault(result) ?? Defaults.Route.Proxy.Upstream.Request.HttpVersion;
        }
    }
}