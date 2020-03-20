namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RequestCreator : IRequestCreator
    {
        private readonly IUriRewriter uriRewriter;
        private readonly IRequestHeaderSetter headerSetter;
        private readonly IRequestContentSetter contentSetter;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUriRewriter uriRewriter,
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
            CallContext context,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));

            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outbound upstream request based on the inbound downstream request.");

            var upstreamRequest = new HttpRequestMessage();

            SetHttpMethod(context, upstreamRequest);
            SetHttpVersion(context, upstreamRequest);
            await SetUriAsync(context, upstreamRequest, cancellationToken);
            await SetHeadersAsync(context, upstreamRequest, cancellationToken);
            await SetContentAsync(context, upstreamRequest, cancellationToken);

            logger.LogDebug("Created an outbound upstream request based on the inbound downstream request.");

            return upstreamRequest;
        }

        private void SetHttpMethod(CallContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Method = Cloudtoid.HttpMethod.Parse(context.Request.Method);
        }

        private void SetHttpVersion(CallContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Version = context.ProxyUpstreamRequestOptions.GetHttpVersion(context);
        }

        private async Task SetUriAsync(
            CallContext context,
            HttpRequestMessage upstreamRequest,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Rewriting the Uri by calling an instance of {0}", uriRewriter.GetType().FullName);

            upstreamRequest.RequestUri = await uriRewriter
                .RewriteUriAsync(context, cancellationToken)
                .TraceOnFaulted(logger, "Failed to rewrite a URI", cancellationToken);

            logger.LogDebug("Rewrote the Uri by calling an instance of {0}", uriRewriter.GetType().FullName);
        }

        private async Task SetHeadersAsync(
            CallContext context,
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
            CallContext context,
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
