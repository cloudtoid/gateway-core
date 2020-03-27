namespace Cloudtoid.Foid.Settings
{
    using System;

    public sealed class UpstreamRequestSettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? httpVersionExpression;
        private readonly string? timeoutInMillisecondsExpression;

        internal UpstreamRequestSettings(
            RouteSettingsContext context,
            string? httpVersionExpression,
            string? timeoutInMillisecondsExpression,
            UpstreamRequestHeadersSettings headers,
            UpstreamRequestSenderSettings sender)
        {
            this.context = context;
            this.httpVersionExpression = httpVersionExpression;
            this.timeoutInMillisecondsExpression = timeoutInMillisecondsExpression;
            Headers = headers;
            Sender = sender;
        }

        public UpstreamRequestHeadersSettings Headers { get; }

        public UpstreamRequestSenderSettings Sender { get; }

        public Version GetHttpVersion(ProxyContext proxyContext)
        {
            var result = context.Evaluate(proxyContext, httpVersionExpression);
            return HttpVersion.ParseOrDefault(result) ?? Defaults.Proxy.Upstream.Request.HttpVersion;
        }

        public TimeSpan GetTimeout(ProxyContext proxyContext)
        {
            var result = context.Evaluate(proxyContext, timeoutInMillisecondsExpression);

            return long.TryParse(result, out var timeout) && timeout > 0
                ? TimeSpan.FromMilliseconds(timeout)
                : Defaults.Proxy.Upstream.Request.Timeout;
        }
    }
}