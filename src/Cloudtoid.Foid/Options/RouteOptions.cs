namespace Cloudtoid.Foid.Options
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using static Contract;
    using Options = FoidOptions;

    public sealed class RouteOptions
    {
        internal RouteOptions(
            OptionsContext context,
            string route,
            Options.RouteOptions options)
        {
            Route = CheckValue(route, nameof(route));

            if (options.Proxy != null && !string.IsNullOrEmpty(options.Proxy.To))
                Proxy = new ProxyOptions(context, options.Proxy);
        }

        public string Route { get; }

        public ProxyOptions? Proxy { get; }

        public struct ExtraHeader
        {
            private readonly OptionsContext context;
            private readonly string[] values;

            internal ExtraHeader(OptionsContext context, string name, string[] values)
            {
                this.context = context;
                Name = name;
                this.values = values;
            }

            public string Name { get; }

            public IEnumerable<string> GetValues(ProxyContext callContext)
            {
                var c = context;
                return values.Select(v => c.Evaluate(callContext, v));
            }
        }

        public sealed class ProxyOptions
        {
            private readonly OptionsContext context;
            private readonly Options.RouteOptions.ProxyOptions options;

            internal ProxyOptions(
                OptionsContext context,
                Options.RouteOptions.ProxyOptions options)
            {
                To = CheckValue(options.To, nameof(options.To));
                this.context = context;
                this.options = options;
                UpstreamRequest = new UpstreamRequestOptions(context, options.UpstreamRequest);
                DownstreamResponse = new DownstreamResponseOptions(context, options.DownstreamResponse);
            }

            public string To { get; }

            public UpstreamRequestOptions UpstreamRequest { get; }

            public DownstreamResponseOptions DownstreamResponse { get; }

            public string GetCorrelationIdHeader(ProxyContext callContext)
            {
                var eval = context.Evaluate(callContext, options.CorrelationIdHeader);
                return string.IsNullOrWhiteSpace(eval)
                    ? Defaults.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                    : eval;
            }

            public sealed class UpstreamRequestOptions
            {
                private readonly OptionsContext context;
                private readonly Options.RouteOptions.ProxyOptions.UpstreamRequestOptions options;

                internal UpstreamRequestOptions(
                    OptionsContext context,
                    Options.RouteOptions.ProxyOptions.UpstreamRequestOptions options)
                {
                    this.context = context;
                    this.options = options;
                    Headers = new HeadersOptions(context, options.Headers);
                    Sender = new SenderOptions(context, options.Sender);
                }

                public HeadersOptions Headers { get; }

                public SenderOptions Sender { get; }

                public Version GetHttpVersion(ProxyContext callContext)
                {
                    var result = context.Evaluate(
                        callContext,
                        options.HttpVersion);

                    return HttpVersion.ParseOrDefault(result) ?? Defaults.Proxy.Upstream.Request.HttpVersion;
                }

                public TimeSpan GetTimeout(ProxyContext callContext)
                {
                    var result = context.Evaluate(
                        callContext,
                        options.TimeoutInMilliseconds);

                    return long.TryParse(result, out var timeout) && timeout > 0
                        ? TimeSpan.FromMilliseconds(timeout)
                        : Defaults.Proxy.Upstream.Request.Timeout;
                }

                public sealed class HeadersOptions
                {
                    private readonly OptionsContext context;
                    private readonly Options.RouteOptions.ProxyOptions.UpstreamRequestOptions.HeadersOptions options;

                    internal HeadersOptions(
                        OptionsContext context,
                        Options.RouteOptions.ProxyOptions.UpstreamRequestOptions.HeadersOptions options)
                    {
                        this.context = context;
                        this.options = options;

                        HeaderNames = new HashSet<string>(
                            options.Headers.Select(h => h.Key).WhereNotNullOrEmpty(),
                            StringComparer.OrdinalIgnoreCase);
                    }

                    public bool AllowHeadersWithEmptyValue
                        => options.AllowHeadersWithEmptyValue;

                    public bool AllowHeadersWithUnderscoreInName
                        => options.AllowHeadersWithUnderscoreInName;

                    public bool IncludeExternalAddress
                        => options.IncludeExternalAddress;

                    public bool IgnoreAllDownstreamHeaders
                        => options.IgnoreAllDownstreamHeaders;

                    public bool IgnoreHost
                        => options.IgnoreHost;

                    public bool IgnoreForwardedFor
                        => options.IgnoreForwardedFor;

                    public bool IgnoreForwardedProtocol
                        => options.IgnoreForwardedProtocol;

                    public bool IgnoreForwardedHost
                        => options.IgnoreForwardedHost;

                    public bool IgnoreCorrelationId
                        => options.IgnoreCorrelationId;

                    public bool IgnoreCallId
                        => options.IgnoreCallId;

                    public ISet<string> HeaderNames { get; }

                    public IEnumerable<ExtraHeader> Headers
                        => options.Headers
                        .Where(h => !string.IsNullOrEmpty(h.Key) && !h.Value.IsNullOrEmpty())
                        .Select(h => new ExtraHeader(context, h.Key, h.Value));

                    public string GetDefaultHost(ProxyContext callContext)
                    {
                        var eval = context.Evaluate(callContext, options.DefaultHost);
                        return string.IsNullOrWhiteSpace(eval)
                            ? Defaults.Proxy.Upstream.Request.Headers.Host
                            : eval;
                    }

                    public bool TryGetProxyName(ProxyContext callContext, [NotNullWhen(true)] out string? proxyName)
                    {
                        var expr = options.ProxyName;
                        if (expr is null)
                        {
                            proxyName = Defaults.Proxy.Upstream.Request.Headers.ProxyName;
                            return true;
                        }

                        var eval = context.Evaluate(callContext, expr);
                        if (string.IsNullOrWhiteSpace(eval))
                        {
                            proxyName = null;
                            return false;
                        }

                        proxyName = eval;
                        return true;
                    }
                }

                public sealed class SenderOptions
                {
                    private readonly OptionsContext context;
                    private readonly Options.RouteOptions.ProxyOptions.UpstreamRequestOptions.SenderOptions options;

                    internal SenderOptions(
                        OptionsContext context,
                        Options.RouteOptions.ProxyOptions.UpstreamRequestOptions.SenderOptions options)
                    {
                        this.context = context;
                        this.options = options;
                    }

                    public bool AllowAutoRedirect
                        => options.AllowAutoRedirect;

                    public bool UseCookies
                        => options.UseCookies;
                }
            }

            public sealed class DownstreamResponseOptions
            {
                internal DownstreamResponseOptions(
                    OptionsContext context,
                    Options.RouteOptions.ProxyOptions.DownstreamResponseOptions options)
                {
                    Headers = new HeadersOptions(context, options.Headers);
                }

                public HeadersOptions Headers { get; }

                public sealed class HeadersOptions
                {
                    private readonly OptionsContext context;
                    private readonly Options.RouteOptions.ProxyOptions.DownstreamResponseOptions.HeadersOptions options;

                    internal HeadersOptions(
                        OptionsContext context,
                        Options.RouteOptions.ProxyOptions.DownstreamResponseOptions.HeadersOptions options)
                    {
                        this.context = context;
                        this.options = options;

                        HeaderNames = new HashSet<string>(
                            options.Headers.Select(h => h.Key).WhereNotNullOrEmpty(),
                            StringComparer.OrdinalIgnoreCase);
                    }

                    public bool AllowHeadersWithEmptyValue
                        => options.AllowHeadersWithEmptyValue;

                    public bool AllowHeadersWithUnderscoreInName
                        => options.AllowHeadersWithUnderscoreInName;

                    public bool IgnoreAllUpstreamHeaders
                        => options.IgnoreAllUpstreamHeaders;

                    public bool IncludeCorrelationId
                        => options.IncludeCorrelationId;

                    public bool IncludeCallId
                        => options.IncludeCallId;

                    public ISet<string> HeaderNames { get; }

                    public IEnumerable<ExtraHeader> Headers
                        => options.Headers
                        .Where(h => !string.IsNullOrEmpty(h.Key) && !h.Value.IsNullOrEmpty())
                        .Select(h => new ExtraHeader(context, h.Key!, h.Value!));
                }
            }
        }
    }
}
