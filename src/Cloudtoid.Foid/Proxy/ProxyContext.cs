namespace Cloudtoid.Foid
{
    using System.Diagnostics;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Settings;
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
                route.Settings.Proxy,
                nameof(route.Settings.Proxy),
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

        internal RouteSettings Settings => Route.Settings;

        internal ProxySettings ProxySettings => Settings.Proxy!;

        internal UpstreamRequestSettings ProxyUpstreamRequestSettings => ProxySettings.UpstreamRequest;

        internal UpstreamRequestHeadersSettings ProxyUpstreamRequestHeadersSettings => ProxyUpstreamRequestSettings.Headers;

        internal DownstreamResponseSettings ProxyDownstreamResponseSettings => ProxySettings.DownstreamResponse;

        internal DownstreamResponseHeadersSettings ProxyDownstreamResponseHeaderSettings => ProxyDownstreamResponseSettings.Headers;
    }
}
