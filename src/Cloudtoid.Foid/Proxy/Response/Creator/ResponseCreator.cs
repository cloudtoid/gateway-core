namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class ResponseCreator : IResponseCreator
    {
        private readonly IResponseHeaderSetter headerSetter;
        private readonly IResponseContentSetter contentSetter;
        private readonly ITrailingHeaderSetter trailingHeaderSetter;
        private readonly ILogger<ResponseCreator> logger;

        public ResponseCreator(
            IResponseHeaderSetter headerSetter,
            IResponseContentSetter contentSetter,
            ITrailingHeaderSetter trailingHeaderSetter,
            ILogger<ResponseCreator> logger)
        {
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.contentSetter = CheckValue(contentSetter, nameof(contentSetter));
            this.trailingHeaderSetter = CheckValue(trailingHeaderSetter, nameof(trailingHeaderSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task CreateResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outbound downstream response based on the inbound upstream request.");

            SetStatusCode(context, upstreamResponse);
            SetReasonPhrase(context, upstreamResponse);
            await SetHeadersAsync(context, upstreamResponse);
            await SetContentAsync(context, upstreamResponse);
            await SetTrailingHeadersAsync(context, upstreamResponse);

            logger.LogDebug("Created an outbound downstream response based on the inbound upstream request.");

            // TODO: Cookies?
        }

        private void SetStatusCode(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError("It is not possible to transfer the HTTP status code from the inbound upstream response to the outbound downstream response. This should never happen!");
                return;
            }

            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        }

        private void SetReasonPhrase(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            var httpVersion = HttpVersion.ParseOrDefault(context.Request.Protocol);
            if (httpVersion is null || httpVersion >= HttpVersion.Version20)
                return; // Reason phrase is not supported by HTTP/2.0 and higher

            var responseFeature = context.Features.Get<IHttpResponseFeature>();
            if (responseFeature is null || responseFeature.ReasonPhrase is null)
                return;

            upstreamResponse.ReasonPhrase = responseFeature.ReasonPhrase;
        }

        private async Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            logger.LogDebug("Appending the HTTP headers to the outbound downstream response");

            await headerSetter
                .SetHeadersAsync(context, upstreamResponse)
                .TraceOnFaulted(logger, "Failed to set the headers on the outbound downstream response", context.RequestAborted);

            logger.LogDebug("Appended the HTTP headers to the outbound downstream response");
        }

        private async Task SetContentAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            logger.LogDebug("Transferring the content of the inbound upstream response to the outbound downstream response");

            await contentSetter
                .SetContentAsync(context, upstreamResponse)
                .TraceOnFaulted(logger, "Failed to set the content body of the outbound downstream response", context.RequestAborted);

            logger.LogDebug("Transferred the content of the inbound upstream response to the outbound downstream response");
        }

        private async Task SetTrailingHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            logger.LogDebug("Appending the trailing headers to the outbound downstream response");

            await trailingHeaderSetter
                .SetHeadersAsync(context, upstreamResponse)
                .TraceOnFaulted(logger, "Failed to set the trailing headers on the outbound downstream response", context.RequestAborted);

            logger.LogDebug("Appended the trailing headers to the outbound downstream response");
        }
    }
}
