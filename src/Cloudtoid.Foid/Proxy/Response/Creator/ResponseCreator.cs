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
        private readonly ILogger<ResponseCreator> logger;

        public ResponseCreator(
            IResponseHeaderSetter headerSetter,
            ILogger<ResponseCreator> logger)
        {
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task CreateResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            logger.LogDebug("Converting the upstream HTTP response to a downstream response.");

            SetStatusCode(context, upstreamResponse);
            SetReasonPhrase(context, upstreamResponse);
            await SetHeadersAsync(context, upstreamResponse);
            await SetContentAsync(context, upstreamResponse);

            // TODO: What are upstreamResponse.Content.Headers vs. upstreamResponse.Headers?
            // Also, do we need to copy both? Others seem to do it!

            // TODO: Cookies, and Trailing headers. How about content length/types/etc?
        }

        private void SetStatusCode(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError("It is not possible to transfer the HTTP status code from the incoming upstream response to the outgoing downstream response. This should never happen!");
                return;
            }

            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        }

        private void SetReasonPhrase(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (context.Request.GetHttpVersion() >= HttpProtocolVersion.Http20)
                return; // ReasonPhrase is no longer supported by HTTP/2.0 and above.

            var responseFeature = context.Features.Get<IHttpResponseFeature>();
            if (responseFeature is null || responseFeature.ReasonPhrase is null)
                return;

            upstreamResponse.ReasonPhrase = responseFeature.ReasonPhrase;
        }

        private async Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            logger.LogDebug("Appending HTTP headers to the outgoing downstream response");

            await headerSetter
                .SetHeadersAsync(context, upstreamResponse)
                .TraceOnFaulted(logger, "Failed to set the headers", context.RequestAborted);

            logger.LogDebug("Appended HTTP headers to the outgoing downstream response");
        }

        private async Task SetContentAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (upstreamResponse.Content is null)
                return;

            logger.LogDebug("Transferring the content of the incoming upstream response to the outgoing downstream response");

            await upstreamResponse.Content.CopyToAsync(context.Response.Body);

            logger.LogDebug("Transferred the content of the incoming upstream response to the outgoing downstream response");
        }
    }
}
