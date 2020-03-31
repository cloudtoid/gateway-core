namespace Cloudtoid.Foid.Upstream
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RequestCreator : IRequestCreator
    {
        private readonly IUrlRewriter uriRewriter;
        private readonly IRequestHeaderSetter headerSetter;
        private readonly IRequestContentSetter contentSetter;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUrlRewriter uriRewriter,
            IRequestHeaderSetter headerSetter,
            IRequestContentSetter contentSetter,
            ILogger<RequestCreator> logger)
        {
            this.uriRewriter = CheckValue(uriRewriter, nameof(uriRewriter));
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.contentSetter = CheckValue(contentSetter, nameof(contentSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task<HttpRequestMessage> CreateRequestAsync(
            ProxyContext context,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));

            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outbound upstream request based on the inbound downstream request.");

            var upstreamRequest = new HttpRequestMessage();

            SetHttpMethod(context, upstreamRequest);
            SetHttpVersion(context, upstreamRequest);
            await SetUrlAsync(context, upstreamRequest, cancellationToken);
            await SetHeadersAsync(context, upstreamRequest, cancellationToken);
            await SetContentAsync(context, upstreamRequest, cancellationToken);

            logger.LogDebug("Created an outbound upstream request based on the inbound downstream request.");

            return upstreamRequest;
        }

        private void SetHttpMethod(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Method = Cloudtoid.HttpMethod.Parse(context.Request.Method);
        }

        private void SetHttpVersion(ProxyContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Version = context.ProxyUpstreamRequestSettings.GetHttpVersion(context);
        }

        private async Task SetUrlAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            var type = uriRewriter.GetType().FullName;
            logger.LogDebug("Rewriting the URL by calling an instance of {0}", type);

            upstreamRequest.RequestUri = await uriRewriter
                .RewriteUrlAsync(context, cancellationToken)
                .TraceOnFaulted(logger, "Failed to rewrite a URL", cancellationToken);

            logger.LogDebug("Rewrote the URL by calling an instance of {0}", type);
        }

        private async Task SetHeadersAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Appending the HTTP headers to the outbound upstream request");

            await headerSetter
                .SetHeadersAsync(context, upstreamRequest, cancellationToken)
                .TraceOnFaulted(logger, "Failed to set the content body of the outbound upstream request", cancellationToken);

            logger.LogDebug("Appended the HTTP headers to the outbound upstream request");
        }

        private async Task SetContentAsync(
            ProxyContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Transferring the content of the inbound downstream request to the outbound upstream request");

            await contentSetter
                .SetContentAsync(context, upstreamRequest, cancellationToken)
                .TraceOnFaulted(logger, "Failed to set the outbound upstream content", cancellationToken);

            logger.LogDebug("Transferred the content of the inbound downstream request to the outbound upstream request");
        }
    }
}
