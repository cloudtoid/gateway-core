namespace Cloudtoid.Foid
{
    using System.Diagnostics;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Options;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    [DebuggerStepThrough]
    public sealed class ProxyContext
    {
        private readonly IHostProvider hostProvider;
        private readonly ITraceIdProvider traceIdProvider;
        private string? host;
        private string? correlationIdHeader;
        private string? correlationId;
        private string? callId;

        internal ProxyContext(
            IHostProvider hostProvider,
            ITraceIdProvider traceIdProvider,
            HttpContext httpContext,
            Route route)
        {
            CheckValue(
                route.Options.Proxy,
                nameof(route.Options.Proxy),
                "This is the actual proxy context. We should never get here if the proxy is null!");

            this.hostProvider = hostProvider;
            this.traceIdProvider = traceIdProvider;
            HttpContext = httpContext;
            Route = route;
        }

        public Route Route { get; }

        public HttpContext HttpContext { get; }

        public HttpRequest Request => HttpContext.Request;

        public HttpResponse Response => HttpContext.Response;

        public string Host
        {
            get
            {
                if (host is null)
                    host = hostProvider.GetHost(this);

                return host;
            }
        }

        public string CorrelationIdHeader
        {
            get
            {
                if (correlationIdHeader is null)
                    correlationIdHeader = traceIdProvider.GetCorrelationIdHeader(this);

                return correlationIdHeader;
            }
        }

        public string CorrelationId
        {
            get
            {
                if (correlationId is null)
                    correlationId = traceIdProvider.GetOrCreateCorrelationId(this);

                return correlationId;
            }
        }

        public string CallId
        {
            get
            {
                if (callId is null)
                    callId = traceIdProvider.CreateCallId(this);

                return callId;
            }
        }

        internal RouteOptions Options => Route.Options;

        internal RouteOptions.ProxyOptions ProxyOptions => Options.Proxy!;

        internal RouteOptions.ProxyOptions.UpstreamRequestOptions ProxyUpstreamRequestOptions => ProxyOptions.UpstreamRequest;

        internal RouteOptions.ProxyOptions.UpstreamRequestOptions.HeadersOptions ProxyUpstreamRequestHeadersOptions => ProxyUpstreamRequestOptions.Headers;

        internal RouteOptions.ProxyOptions.DownstreamResponseOptions ProxyDownstreamResponseOptions => ProxyOptions.DownstreamResponse;

        internal RouteOptions.ProxyOptions.DownstreamResponseOptions.HeadersOptions ProxyDownstreamResponseHeaderOptions => ProxyDownstreamResponseOptions.Headers;
    }
}
