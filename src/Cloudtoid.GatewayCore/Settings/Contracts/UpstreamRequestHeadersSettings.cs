namespace Cloudtoid.GatewayCore.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public sealed class UpstreamRequestHeadersSettings
    {
        private readonly RouteSettingsContext context;
        private readonly string? defaultHostExpression;
        private readonly string? proxyNameExpression;

        internal UpstreamRequestHeadersSettings(
            RouteSettingsContext context,
            string? defaultHostExpression,
            string? proxyNameExpression,
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName,
            bool includeExternalAddress,
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
            this.proxyNameExpression = proxyNameExpression;
            AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
            AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
            IncludeExternalAddress = includeExternalAddress;
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

        public IReadOnlyList<HeaderOverride> Overrides { get; }

        public ISet<string> OverrideNames { get; }

        public string GetDefaultHost(ProxyContext proxyContext)
        {
            var eval = context.Evaluate(proxyContext, defaultHostExpression);
            return string.IsNullOrWhiteSpace(eval)
                ? Defaults.Route.Proxy.Upstream.Request.Headers.Host
                : eval;
        }

        public bool TryGetProxyName(
            ProxyContext proxyContext,
            [NotNullWhen(true)] out string? proxyName)
        {
            if (proxyNameExpression is null)
            {
                proxyName = Defaults.Route.Proxy.Upstream.Request.Headers.ProxyName;
                return true;
            }

            var eval = context.Evaluate(proxyContext, proxyNameExpression);
            if (string.IsNullOrWhiteSpace(eval))
            {
                proxyName = null;
                return false;
            }

            proxyName = eval;
            return true;
        }
    }
}