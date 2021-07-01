using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Downstream
{
    internal sealed class ResponseSender : IResponseSender
    {
        private readonly IResponseHeaderSetter headerSetter;
        private readonly IResponseContentSetter contentSetter;
        private readonly ITrailingHeaderSetter trailingHeaderSetter;
        private readonly ILogger<ResponseSender> logger;

        public ResponseSender(
            IResponseHeaderSetter headerSetter,
            IResponseContentSetter contentSetter,
            ITrailingHeaderSetter trailingHeaderSetter,
            ILogger<ResponseSender> logger)
        {
            this.headerSetter = CheckValue(headerSetter, nameof(headerSetter));
            this.contentSetter = CheckValue(contentSetter, nameof(contentSetter));
            this.trailingHeaderSetter = CheckValue(trailingHeaderSetter, nameof(trailingHeaderSetter));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task SendResponseAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Creating an outbound downstream response based on the inbound upstream request.");

            SetStatusCode(context, upstreamResponse);
            SetReasonPhrase(context, upstreamResponse);
            await SetHeadersAsync(context, upstreamResponse, cancellationToken);
            await SetContentAsync(context, upstreamResponse, cancellationToken);
            await SetTrailingHeadersAsync(context, upstreamResponse, cancellationToken);

            logger.LogDebug("Created an outbound downstream response based on the inbound upstream request.");
        }

        private void SetStatusCode(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError("It is not possible to transfer the HTTP status code from the inbound upstream response to the outbound downstream response. This should never happen!");
                return;
            }

            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        }

        private static void SetReasonPhrase(ProxyContext context, HttpResponseMessage upstreamResponse)
        {
            if (context.RequestHttpVersion >= HttpVersion.Version20)
                return; // Reason phrase is not supported by HTTP/2 and higher

            var responseFeature = context.HttpContext.Features.Get<IHttpResponseFeature>();
            if (responseFeature is null || responseFeature.ReasonPhrase is null)
                return;

            upstreamResponse.ReasonPhrase = responseFeature.ReasonPhrase;
        }

        private async Task SetHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Appending the HTTP headers to the outbound downstream response");

            await headerSetter
                .SetHeadersAsync(context, upstreamResponse, cancellationToken)
                .TraceOnFaulted(logger, "Failed to set the headers on the outbound downstream response", cancellationToken);

            logger.LogDebug("Appended the HTTP headers to the outbound downstream response");
        }

        private async Task SetContentAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Transferring the content of the inbound upstream response to the outbound downstream response");

            await contentSetter
                .SetContentAsync(context, upstreamResponse, cancellationToken)
                .TraceOnFaulted(logger, "Failed to set the content body of the outbound downstream response", cancellationToken);

            logger.LogDebug("Transferred the content of the inbound upstream response to the outbound downstream response");
        }

        private async Task SetTrailingHeadersAsync(
            ProxyContext context,
            HttpResponseMessage upstreamResponse,
            CancellationToken cancellationToken)
        {
            logger.LogDebug("Appending the trailing headers to the outbound downstream response");

            await trailingHeaderSetter
                .SetHeadersAsync(context, upstreamResponse, cancellationToken)
                .TraceOnFaulted(logger, "Failed to set the trailing headers on the outbound downstream response", cancellationToken);

            logger.LogDebug("Appended the trailing headers to the outbound downstream response");
        }
    }
}
