namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class UpstreamRequestHeadersSettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? defaultHostExpression;

        internal UpstreamRequestHeadersSettings(
            RouteSettingsContext context,
            string? defaultHostExpression,
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName,
            bool includeExternalAddress,
            bool includeProxyName,
            bool ignoreAllDownstreamHeaders,
            bool ignoreHost,
            bool ignoreCorrelationId,
            bool ignoreCallId,
            bool ignoreForwarded,
            bool useXForwarded,
            IReadOnlyList<HeaderOverride> overrides)
        {
            this.context = context;
            this.defaultHostExpression = defaultHostExpression;
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IncludeExternalAddress = includeExternalAddress;
            IncludeProxyName = includeProxyName;
            IgnoreAllDownstreamHeaders = ignoreAllDownstreamHeaders;
            IgnoreHost = ignoreHost;
            IgnoreCorrelationId = ignoreCorrelationId;
            IgnoreCallId = ignoreCallId;
            IgnoreForwarded = ignoreForwarded;
            UseXForwarded = useXForwarded;
            Overrides = overrides;
            OverrideNames = new HashSet<string>(
                overrides.Select(h => h.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool AllowHeadersWithEmptyValue { get; }

        public bool AllowHeadersWithUnderscoreInName { get; }

        public bool IncludeExternalAddress { get; }

        public bool IgnoreAllDownstreamHeaders { get; }

        public bool IgnoreHost { get; }

        public bool IgnoreCorrelationId { get; }

        public bool IgnoreCallId { get; }

        public bool IgnoreForwarded { get; }

        public bool UseXForwarded { get; }

        public bool IncludeProxyName { get; }

        public IReadOnlyList<HeaderOverride> Overrides { get; }

        public ISet<string> OverrideNames { get; }

        public string GetDefaultHost(ProxyContext proxyContext)
        {
            var eval = context.Evaluate(proxyContext, defaultHostExpression);
            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.Upstream.Request.Headers.Host
                : eval;
        }
    }
}