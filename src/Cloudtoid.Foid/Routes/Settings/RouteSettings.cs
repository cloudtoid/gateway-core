namespace Cloudtoid.Foid.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public sealed class RouteSettings
    {
        internal RouteSettings(
            string route,
            ProxySettings? proxySettings)
        {
            Route = route;
            Proxy = proxySettings;
        }

        public string Route { get; }

        public ProxySettings? Proxy { get; }

        public sealed class HeaderOverride
        {
            private readonly RouteSettingsContext context;
            private readonly string[] values;

            internal HeaderOverride(
                RouteSettingsContext context,
                string name,
                string[] values)
            {
                this.context = context;
                Name = name;
                this.values = values;
            }

            public string Name { get; }

            public bool HasValues => values.Length > 0;

            public IEnumerable<string> GetValues(ProxyContext proxyContext)
                => values.Select(v => context.Evaluate(proxyContext, v));
        }

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
                    ? Defaults.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                    : eval;
            }

            public sealed class UpstreamRequestSettings
            {
                private readonly RouteSettingsContext context;
                private readonly string? httpVersionExpression;
                private readonly string? timeoutInMillisecondsExpression;

                internal UpstreamRequestSettings(
                    RouteSettingsContext context,
                    string? httpVersionExpression,
                    string? timeoutInMillisecondsExpression,
                    HeadersSettings headers,
                    SenderSettings sender)
                {
                    this.context = context;
                    this.httpVersionExpression = httpVersionExpression;
                    this.timeoutInMillisecondsExpression = timeoutInMillisecondsExpression;
                    Headers = headers;
                    Sender = sender;
                }

                public HeadersSettings Headers { get; }

                public SenderSettings Sender { get; }

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

                public sealed class HeadersSettings
                {
                    private readonly RouteSettingsContext context;
                    private readonly string? defaultHostExpression;
                    private readonly string? proxyNameExpression;

                    internal HeadersSettings(
                        RouteSettingsContext context,
                        string? defaultHostExpression,
                        string? proxyNameExpression,
                        bool allowHeadersWithEmptyValue,
                        bool allowHeadersWithUnderscoreInName,
                        bool includeExternalAddress,
                        bool ignoreAllDownstreamHeaders,
                        bool ignoreHost,
                        bool ignoreForwardedFor,
                        bool ignoreForwardedProtocol,
                        bool ignoreForwardedHost,
                        bool ignoreCorrelationId,
                        bool ignoreCallId,
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
                        IgnoreForwardedFor = ignoreForwardedFor;
                        IgnoreForwardedProtocol = ignoreForwardedProtocol;
                        IgnoreForwardedHost = ignoreForwardedHost;
                        IgnoreCorrelationId = ignoreCorrelationId;
                        IgnoreCallId = ignoreCallId;
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

                    public bool IgnoreForwardedFor { get; }

                    public bool IgnoreForwardedProtocol { get; }

                    public bool IgnoreForwardedHost { get; }

                    public bool IgnoreCorrelationId { get; }

                    public bool IgnoreCallId { get; }

                    public IReadOnlyList<HeaderOverride> Overrides { get; }

                    public ISet<string> OverrideNames { get; }

                    public string GetDefaultHost(ProxyContext proxyContext)
                    {
                        var eval = context.Evaluate(proxyContext, defaultHostExpression);
                        return string.IsNullOrWhiteSpace(eval)
                            ? Defaults.Proxy.Upstream.Request.Headers.Host
                            : eval;
                    }

                    public bool TryGetProxyName(
                        ProxyContext proxyContext,
                        [NotNullWhen(true)] out string? proxyName)
                    {
                        if (proxyNameExpression is null)
                        {
                            proxyName = Defaults.Proxy.Upstream.Request.Headers.ProxyName;
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

                public sealed class SenderSettings
                {
                    internal SenderSettings(
                        bool allowAutoRedirect,
                        bool useCookies)
                    {
                        AllowAutoRedirect = allowAutoRedirect;
                        UseCookies = useCookies;
                    }

                    public bool AllowAutoRedirect { get; }

                    public bool UseCookies { get; }
                }
            }

            public sealed class DownstreamResponseSettings
            {
                internal DownstreamResponseSettings(
                    HeadersSettings headers)
                {
                    Headers = headers;
                }

                public HeadersSettings Headers { get; }

                public sealed class HeadersSettings
                {
                    internal HeadersSettings(
                        bool allowHeadersWithEmptyValue,
                        bool allowHeadersWithUnderscoreInName,
                        bool ignoreAllUpstreamHeaders,
                        bool includeCorrelationId,
                        bool includeCallId,
                        IReadOnlyList<HeaderOverride> overrides)
                    {
                        AllowHeadersWithEmptyValue = allowHeadersWithEmptyValue;
                        AllowHeadersWithUnderscoreInName = allowHeadersWithUnderscoreInName;
                        IgnoreAllUpstreamHeaders = ignoreAllUpstreamHeaders;
                        IncludeCorrelationId = includeCorrelationId;
                        IncludeCallId = includeCallId;
                        Overrides = overrides;
                        OverrideNames = new HashSet<string>(
                            overrides.Select(h => h.Name),
                            StringComparer.OrdinalIgnoreCase);
                    }

                    public bool AllowHeadersWithEmptyValue { get; }

                    public bool AllowHeadersWithUnderscoreInName { get; }

                    public bool IgnoreAllUpstreamHeaders { get; }

                    public bool IncludeCorrelationId { get; }

                    public bool IncludeCallId { get; }

                    public IReadOnlyList<HeaderOverride> Overrides { get; }

                    public ISet<string> OverrideNames { get; }
                }
            }
        }
    }
}
