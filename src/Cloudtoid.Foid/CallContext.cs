namespace Cloudtoid.Foid
{
    using System.Diagnostics;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Options;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;

    ////[DebuggerStepThrough]
    public sealed class CallContext
    {
        private readonly IHostProvider hostProvider;
        private readonly ITraceIdProvider traceIdProvider;
        private string? host;
        private string? correlationIdHeader;
        private string? correlationId;
        private string? callId;

        internal CallContext(
            IHostProvider hostProvider,
            ITraceIdProvider traceIdProvider,
            HttpContext httpContext,
            Route route)
        {
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

        internal RouteOptions.ProxyOptions ProxyOptions => Options.Proxy;

        internal RouteOptions.ProxyOptions.UpstreamOptions ProxyUpstreamOptions => ProxyOptions.Upstream;

        internal RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions ProxyUpstreamRequestOptions => ProxyUpstreamOptions.Request;

        internal RouteOptions.ProxyOptions.UpstreamOptions.RequestOptions.HeadersOptions ProxyUpstreamRequestHeadersOptions => ProxyUpstreamRequestOptions.Headers;

        internal RouteOptions.ProxyOptions.DownstreamOptions ProxyDownstreamOptions => ProxyOptions.Downstream;

        internal RouteOptions.ProxyOptions.DownstreamOptions.ResponseOptions ProxyDownstreamResponseOptions => ProxyDownstreamOptions.Response;

        internal RouteOptions.ProxyOptions.DownstreamOptions.ResponseOptions.HeadersOptions ProxyDownstreamResponseHeaderOptions => ProxyDownstreamResponseOptions.Headers;
    }
}
