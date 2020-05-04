namespace Cloudtoid.GatewayCore
{
    using System;
    using System.Diagnostics;
    using Cloudtoid.GatewayCore.Host;
    using Cloudtoid.GatewayCore.Settings;
    using Cloudtoid.GatewayCore.Trace;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    [DebuggerStepThrough]
    public sealed class ProxyContext
    {
        private readonly IHostProvider hostProvider;
        private readonly ITraceIdProvider traceIdProvider;
        private string? host;
        private string? proxyName;
        private string? correlationIdHeader;
        private string? correlationId;
        private string? callId;
        private Version? requestHttpVersion;

        internal ProxyContext(
            IHostProvider hostProvider,
            ITraceIdProvider traceIdProvider,
            HttpContext httpContext,
            Route route)
        {
            ProxySettings = CheckValue(
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
            => host is null ? host = hostProvider.GetHost(this) : host;

        public string ProxyName
            => proxyName is null ? proxyName = ProxySettings.GetProxyName(this) : proxyName;

        public string CorrelationIdHeader
            => correlationIdHeader is null ? correlationIdHeader = traceIdProvider.GetCorrelationIdHeader(this) : correlationIdHeader;

        public string CorrelationId
            => correlationId is null ? correlationId = traceIdProvider.GetOrCreateCorrelationId(this) : correlationId;

        public string CallId
            => callId is null ? callId = traceIdProvider.CreateCallId(this) : callId;

        public Version RequestHttpVersion
        {
            get
            {
                if (requestHttpVersion is null)
                    requestHttpVersion = HttpVersion.ParseOrDefault(Request.Protocol) ?? HttpVersion.Version11;

                return requestHttpVersion;
            }
        }

        internal RouteSettings Settings => Route.Settings;

        internal ProxySettings ProxySettings { get; }

        internal UpstreamRequestSettings ProxyUpstreamRequestSettings => ProxySettings.UpstreamRequest;

        internal UpstreamRequestHeadersSettings ProxyUpstreamRequestHeadersSettings => ProxyUpstreamRequestSettings.Headers;

        internal UpstreamRequestSenderSettings ProxyUpstreamRequestSenderSettings => ProxyUpstreamRequestSettings.Sender;

        internal DownstreamResponseSettings ProxyDownstreamResponseSettings => ProxySettings.DownstreamResponse;

        internal DownstreamResponseHeadersSettings ProxyDownstreamResponseHeaderSettings => ProxyDownstreamResponseSettings.Headers;
    }
}
