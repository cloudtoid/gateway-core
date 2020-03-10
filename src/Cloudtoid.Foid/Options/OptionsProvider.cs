namespace Cloudtoid.Foid
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;

    public sealed class OptionsProvider
    {
        private readonly Context context;

        public OptionsProvider(
            IExpressionEvaluator evaluator,
            IOptionsMonitor<FoidOptions> options)
        {
            CheckValue(evaluator, nameof(evaluator));
            CheckValue(options, nameof(options));

            context = new Context(evaluator, options);
            Proxy = new ProxyOptions(context);
        }

        public ProxyOptions Proxy { get; }

        public struct ExtraHeader
        {
            private readonly Context context;
            private readonly string[] values;

            internal ExtraHeader(Context context, string name, string[] values)
            {
                this.context = context;
                Name = name;
                this.values = values;
            }

            public string Name { get; }

            public IEnumerable<string> GetValues(HttpContext httpContext)
            {
                var c = context;
                return values.Select(v => c.Evaluate(httpContext, v)).WhereNotNull();
            }
        }

        public sealed class ProxyOptions
        {
            internal ProxyOptions(Context context)
            {
                Upstream = new UpstreamOptions(context);
                Downstream = new DownstreamOptions(context);
            }

            public UpstreamOptions Upstream { get; }

            public DownstreamOptions Downstream { get; }

            public sealed class UpstreamOptions
            {
                internal UpstreamOptions(Context context)
                {
                    Request = new RequestOptions(context);
                }

                public RequestOptions Request { get; }

                public sealed class RequestOptions
                {
                    private readonly Context context;

                    internal RequestOptions(Context context)
                    {
                        this.context = context;
                        Headers = new HeadersOptions(context);
                    }

                    public HeadersOptions Headers { get; }

                    public TimeSpan GetTimeout(HttpContext httpContext)
                    {
                        var result = context.Evaluate(httpContext, context.UpstreamRequest.TimeoutInMilliseconds);
                        return long.TryParse(result, out var timeout)
                            ? TimeSpan.FromMilliseconds(timeout)
                            : Defaults.Proxy.Upstream.Request.Timeout;
                    }

                    public sealed class HeadersOptions
                    {
                        private readonly Context context;

                        internal HeadersOptions(Context context)
                        {
                            this.context = context;
                            HeaderNames = new HashSet<string>(
                                context.UpstreamRequestHeaders.Headers.Select(h => h.Name).WhereNotNullOrEmpty(),
                                StringComparer.OrdinalIgnoreCase);
                        }

                        public bool AllowHeadersWithEmptyValue
                            => context.UpstreamRequestHeaders.AllowHeadersWithEmptyValue;

                        public bool AllowHeadersWithUnderscoreInName
                            => context.UpstreamRequestHeaders.AllowHeadersWithUnderscoreInName;

                        public bool IncludeExternalAddress
                            => context.UpstreamRequestHeaders.IncludeExternalAddress;

                        public bool IgnoreAllDownstreamRequestHeaders
                            => context.UpstreamRequestHeaders.IgnoreAllDownstreamRequestHeaders;

                        public bool IgnoreHost
                            => context.UpstreamRequestHeaders.IgnoreHost;

                        public bool IgnoreClientAddress
                            => context.UpstreamRequestHeaders.IgnoreClientAddress;

                        public bool IgnoreClientProtocol
                            => context.UpstreamRequestHeaders.IgnoreClientProtocol;

                        public bool IgnoreCorrelationId
                            => context.UpstreamRequestHeaders.IgnoreCorrelationId;

                        public bool IgnoreCallId
                            => context.UpstreamRequestHeaders.IgnoreCallId;

                        public ISet<string> HeaderNames { get; }

                        public IEnumerable<ExtraHeader> Headers
                            => context.UpstreamRequestHeaders.Headers
                            .Where(h => !string.IsNullOrEmpty(h.Name) && !h.Values.IsNullOrEmpty())
                            .Select(h => new ExtraHeader(context, h.Name!, h.Values!));

                        public string GetCorrelationIdHeader(HttpContext httpContext)
                        {
                            var eval = context.Evaluate(httpContext, context.UpstreamRequestHeaders.CorrelationIdHeader);
                            return string.IsNullOrWhiteSpace(eval)
                                ? Defaults.Proxy.Upstream.Request.Headers.CorrelationIdHeader
                                : eval;
                        }

                        public string GetDefaultHost(HttpContext httpContext)
                        {
                            var eval = context.Evaluate(httpContext, context.UpstreamRequestHeaders.DefaultHost);
                            return string.IsNullOrWhiteSpace(eval)
                                ? Defaults.Proxy.Upstream.Request.Headers.Host
                                : eval;
                        }

                        public bool TryGetProxyName(HttpContext httpContext, [NotNullWhen(true)] out string? proxyName)
                        {
                            var expr = context.UpstreamRequestHeaders.ProxyName;
                            if (expr is null)
                            {
                                proxyName = Defaults.Proxy.Upstream.Request.Headers.ProxyName;
                                return true;
                            }

                            var eval = context.Evaluate(httpContext, expr);
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
            }

            public sealed class DownstreamOptions
            {
                internal DownstreamOptions(Context context)
                {
                    Response = new ResponseOptions(context);
                }

                public ResponseOptions Response { get; }

                public sealed class ResponseOptions
                {
                    internal ResponseOptions(Context context)
                    {
                        Headers = new HeadersOptions(context);
                    }

                    public HeadersOptions Headers { get; }

                    public sealed class HeadersOptions
                    {
                        private readonly Context context;

                        internal HeadersOptions(Context context)
                        {
                            this.context = context;
                            HeaderNames = new HashSet<string>(
                                context.DownstreamResponseHeaders.Headers.Select(h => h.Name).WhereNotNullOrEmpty(),
                                StringComparer.OrdinalIgnoreCase);
                        }

                        public bool AllowHeadersWithEmptyValue
                            => context.DownstreamResponseHeaders.AllowHeadersWithEmptyValue;

                        public bool AllowHeadersWithUnderscoreInName
                            => context.DownstreamResponseHeaders.AllowHeadersWithUnderscoreInName;

                        public bool IgnoreAllUpstreamResponseHeaders
                            => context.DownstreamResponseHeaders.IgnoreAllUpstreamResponseHeaders;

                        public ISet<string> HeaderNames { get; }

                        public IEnumerable<ExtraHeader> Headers
                            => context.DownstreamResponseHeaders.Headers
                            .Where(h => !string.IsNullOrEmpty(h.Name) && !h.Values.IsNullOrEmpty())
                            .Select(h => new ExtraHeader(context, h.Name!, h.Values!));
                    }
                }
            }
        }

        internal sealed class Context
        {
            private readonly IExpressionEvaluator evaluator;
            private readonly IOptionsMonitor<FoidOptions> options;

            internal Context(
                IExpressionEvaluator evaluator,
                IOptionsMonitor<FoidOptions> options)
            {
                this.evaluator = evaluator;
                this.options = options;
            }

            internal FoidOptions.ProxyOptions Proxy => options.CurrentValue.Proxy;

            internal FoidOptions.ProxyOptions.UpstreamOptions Upstream => Proxy.Upstream;

            internal FoidOptions.ProxyOptions.UpstreamOptions.RequestOptions UpstreamRequest => Upstream.Request;

            internal FoidOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions UpstreamRequestHeaders => UpstreamRequest.Headers;

            internal FoidOptions.ProxyOptions.DownstreamOptions Downstream => Proxy.Downstream;

            internal FoidOptions.ProxyOptions.DownstreamOptions.ResponseOptions DownstreamResponse => Downstream.Response;

            internal FoidOptions.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions DownstreamResponseHeaders => DownstreamResponse.Headers;

            internal string? Evaluate(HttpContext context, string? expression)
                => evaluator.Evaluate(context, expression);
        }
    }
}
