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
            Proxy = new ProxyOptions(context, CheckValue(options.Proxy, nameof(options.Proxy)));
        }

        public string Route { get; }

        public ProxyOptions Proxy { get; }

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

            public IEnumerable<string> GetValues(CallContext callContext)
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
                this.context = context;
                this.options = options;
                Upstream = new UpstreamOptions(context, options.Upstream);
                Downstream = new DownstreamOptions(context, options.Downstream);
            }

            public UpstreamOptions Upstream { get; }

            public DownstreamOptions Downstream { get; }

            public string GetCorrelationIdHeader(CallContext callContext)
            {
                var eval = context.Evaluate(callContext, options.CorrelationIdHeader);
                return string.IsNullOrWhiteSpace(eval)
                    ? Defaults.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                    : eval;
            }

            public sealed class UpstreamOptions
            {
                internal UpstreamOptions(
                    OptionsContext context,
                    Options.RouteOptions.ProxyOptions.UpstreamOptions options)
                {
                    Request = new RequestOptions(context, options.Request);
                }

                public RequestOptions Request { get; }

                public sealed class RequestOptions
                {
                    private readonly OptionsContext context;
                    private readonly Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions options;

                    internal RequestOptions(
                        OptionsContext context,
                        Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions options)
                    {
                        this.context = context;
                        this.options = options;
                        Headers = new HeadersOptions(context, options.Headers);
                        Sender = new SenderOptions(context, options.Sender);
                    }

                    public HeadersOptions Headers { get; }

                    public SenderOptions Sender { get; }

                    public Version GetHttpVersion(CallContext callContext)
                    {
                        var result = context.Evaluate(
                            callContext,
                            options.HttpVersion);

                        return HttpVersion.ParseOrDefault(result) ?? Defaults.Proxy.Upstream.Request.HttpVersion;
                    }

                    public TimeSpan GetTimeout(CallContext callContext)
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
                        private readonly Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions options;

                        internal HeadersOptions(
                            OptionsContext context,
                            Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions options)
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

                        public string GetDefaultHost(CallContext callContext)
                        {
                            var eval = context.Evaluate(callContext, options.DefaultHost);
                            return string.IsNullOrWhiteSpace(eval)
                                ? Defaults.Proxy.Upstream.Request.Headers.Host
                                : eval;
                        }

                        public bool TryGetProxyName(CallContext callContext, [NotNullWhen(true)] out string? proxyName)
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
                        private readonly Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions.SenderOptions options;

                        internal SenderOptions(
                            OptionsContext context,
                            Options.RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions.SenderOptions options)
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
            }

            public sealed class DownstreamOptions
            {
                internal DownstreamOptions(
                    OptionsContext context,
                    Options.RouteOptions.ProxyOptions.DownstreamOptions options)
                {
                    Response = new ResponseOptions(context, options.Response);
                }

                public ResponseOptions Response { get; }

                public sealed class ResponseOptions
                {
                    internal ResponseOptions(
                        OptionsContext context,
                        Options.RouteOptions.ProxyOptions.DownstreamOptions.ResponseOptions options)
                    {
                        Headers = new HeadersOptions(context, options.Headers);
                    }

                    public HeadersOptions Headers { get; }

                    public sealed class HeadersOptions
                    {
                        private readonly OptionsContext context;
                        private readonly Options.RouteOptions.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions options;

                        internal HeadersOptions(
                            OptionsContext context,
                            Options.RouteOptions.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions options)
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
}
