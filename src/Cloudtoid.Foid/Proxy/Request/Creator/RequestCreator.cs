namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RequestCreator : IRequestCreator
    {
        private readonly IUriRewriter uriRewriter;
        private readonly IRequestHeaderSetter headerSetter;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUriRewriter uriRewriter,
            IRequestHeaderSetter headerSetter,
            ILogger<RequestCreator> logger)
        {
            this.uriRewriter = CheckValue(uriRewriter, nameof(uriRewriter));
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task<HttpRequestMessage> CreateRequestAsync(HttpContext context)
        {
            CheckValue(context, nameof(context));

            context.RequestAborted.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outgoing upstream HTTP request based on the incoming downstream HTTP request.");

            var upstreamRequest = new HttpRequestMessage();

            SetHttpMethod(context, upstreamRequest);
            await SetUriAsync(context, upstreamRequest);
            await SetHeadersAsync(context, upstreamRequest);
            SetContent(context, upstreamRequest);

            logger.LogDebug("Creating an outgoing upstream HTTP request based on the incoming downstream HTTP request.");

            return upstreamRequest;
        }

        private void SetHttpMethod(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Method = HttpUtil.GetHttpMethod(context.Request.Method);
        }

        private async Task SetUriAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            logger.LogDebug("Rewriting the uri by calling an instance of {0}", uriRewriter.GetType().FullName);

            upstreamRequest.RequestUri = await uriRewriter
                .RewriteUriAsync(context)
                .TraceOnFaulted(logger, "Failed to rewrite a URI", context.RequestAborted);

            logger.LogDebug("Rewrote the uri by calling an instance of {0}", uriRewriter.GetType().FullName);
        }

        private async Task SetHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            logger.LogDebug("Appending HTTP headers to the outgoing upstream request");

            await headerSetter
                .SetHeadersAsync(context, upstreamRequest)
                .TraceOnFaulted(logger, "Failed to set the headers", context.RequestAborted);

            logger.LogDebug("Appended HTTP headers to the outgoing upstream request");
        }

        private void SetContent(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            logger.LogDebug("Transferring the content of the incoming downstream request to the outgoing upstream request");

            var body = context.Request.Body;
            if (body is null || !body.CanRead || context.Request.ContentLength <= 0)
            {
                logger.LogDebug("The incoming downstream request does not have a body, it is not readable, or its content length is zero.");
                return;
            }

            if (body.CanSeek && body.Position != 0)
            {
                logger.LogDebug("The incoming downstream request has a seekable body stream. Resetting the stream to the begining.");
                body.Position = 0;
            }

            upstreamRequest.Content = new StreamContent(body);

            logger.LogDebug("Transferred the content of the incoming downstream request to the outgoing upstream request");
        }
    }
}
