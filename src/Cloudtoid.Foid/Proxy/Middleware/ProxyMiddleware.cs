namespace Cloudtoid.Foid.Proxy
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloudtoid;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Routes;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class ProxyMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IRequestCreator requestCreator;
        private readonly IRequestSender sender;
        private readonly IResponseSender responseSender;
        private readonly IRouteProvider routeProvider;
        private readonly IHostProvider hostProvider;
        private readonly ITraceIdProvider traceIdProvider;
        private readonly ILogger<ProxyMiddleware> logger;

        public ProxyMiddleware(
            RequestDelegate next,
            IRequestCreator requestCreator,
            IRequestSender sender,
            IResponseSender responseSender,
            IRouteProvider routeProvider,
            IHostProvider hostProvider,
            ITraceIdProvider traceIdProvider,
            ILogger<ProxyMiddleware> logger)
        {
            this.next = CheckValue(next, nameof(next));
            this.requestCreator = CheckValue(requestCreator, nameof(requestCreator));
            this.sender = CheckValue(sender, nameof(sender));
            this.responseSender = CheckValue(responseSender, nameof(responseSender));
            this.routeProvider = CheckValue(routeProvider, nameof(routeProvider));
            this.hostProvider = CheckValue(hostProvider, nameof(hostProvider));
            this.traceIdProvider = CheckValue(traceIdProvider, nameof(traceIdProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        [SuppressMessage("Style", "VSTHRD200:Use Async suffix for async methods", Justification = "Implementing an ASP.NET middleware. This signature cannot be changed.")]
        public async Task Invoke(HttpContext httpContext)
        {
            CheckValue(httpContext, nameof(httpContext));

            logger.LogDebug("Reverse proxy received a new inbound downstream {0} request.", httpContext.Request.Method);

            var cancellationToken = httpContext.RequestAborted;
            cancellationToken.ThrowIfCancellationRequested();

            if (routeProvider.TryGetRoute(httpContext, out var route))
            {
                var context = new CallContext(
                    hostProvider,
                    traceIdProvider,
                    httpContext,
                    route);

                var upstreamRequest = await CreateUpstreamRequestAsync(context, cancellationToken);
                var upstreamResponse = await SendUpstreamRequestAsync(context, upstreamRequest, cancellationToken);
                await SendDownstreamResponseAsync(context, upstreamResponse, cancellationToken);
            }

            await next.Invoke(httpContext);
        }

        private async Task<HttpRequestMessage> CreateUpstreamRequestAsync(
            CallContext context,
            CancellationToken cancellationToken)
        {
            return await requestCreator
                .CreateRequestAsync(context, cancellationToken)
                .TraceOnFaulted(logger, "Failed to create an outbound upstream request.", cancellationToken);
        }

        private async Task<HttpResponseMessage> SendUpstreamRequestAsync(
            CallContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            var upstreamTimeout = context.ProxyUpstreamRequestOptions.GetTimeout(context);

            try
            {
                return await Async
                    .WithTimeout(sender.SendAsync, upstreamRequest, upstreamTimeout, cancellationToken)
                    .TraceOnFaulted(logger, "Failed to forward the request to the upstream system.", cancellationToken);
            }
            catch (HttpRequestException hre)
            {
                throw new ProxyException(HttpStatusCode.BadGateway, hre);
            }
        }

        private async Task SendDownstreamResponseAsync(
            CallContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            await responseSender
                .SendResponseAsync(context, upstreamResponse, cancellationToken)
                .TraceOnFaulted(logger, "Failed to convert and send the downstream response message.", cancellationToken);
        }
    }
}
