namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RequestCreator : IRequestCreator
    {
        private readonly IUriRewriter uriRewriter;
        private readonly IRequestHeaderSetter headerSetter;
        private readonly IRequestContentSetter contentSetter;
        private readonly OptionsProvider options;
        private readonly ILogger<RequestCreator> logger;

        public RequestCreator(
            IUriRewriter uriRewriter,
            IRequestHeaderSetter headerSetter,
            IRequestContentSetter contentSetter,
            OptionsProvider options,
            ILogger<RequestCreator> logger)
        {
            this.uriRewriter = CheckValue(uriRewriter, nameof(uriRewriter));
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.contentSetter = CheckValue(contentSetter, nameof(contentSetter));
            this.options = CheckValue(options, nameof(options));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task<HttpRequestMessage> CreateRequestAsync(HttpContext context)
        {
            CheckValue(context, nameof(context));

            context.RequestAborted.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outbound upstream request based on the inbound downstream request.");

            var upstreamRequest = new HttpRequestMessage();

            SetHttpMethod(context, upstreamRequest);
            SetHttpVersion(context, upstreamRequest);
            await SetUriAsync(context, upstreamRequest);
            await SetHeadersAsync(context, upstreamRequest);
            await SetContentAsync(context, upstreamRequest);

            logger.LogDebug("Created an outbound upstream request based on the inbound downstream request.");

            return upstreamRequest;
        }

        private void SetHttpMethod(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Method = Cloudtoid.HttpMethod.Parse(context.Request.Method);
        }

        private void SetHttpVersion(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            upstreamRequest.Version = options.Proxy.Upstream.Request.GetHttpVersion(context);
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
            logger.LogDebug("Appending the HTTP headers to the outbound upstream request");

            await headerSetter
                .SetHeadersAsync(context, upstreamRequest)
                .TraceOnFaulted(logger, "Failed to set the content body of the outbound upstream request", context.RequestAborted);

            logger.LogDebug("Appended the HTTP headers to the outbound upstream request");
        }

        private async Task SetContentAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            logger.LogDebug("Transferring the content of the inbound downstream request to the outbound upstream request");

            await contentSetter
                .SetContentAsync(context, upstreamRequest)
                .TraceOnFaulted(logger, "Failed to set the outbound upstream content", context.RequestAborted);

            logger.LogDebug("Transferred the content of the inbound downstream request to the outbound upstream request");
        }
    }
}
