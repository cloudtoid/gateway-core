namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class ResponseCreator : IResponseCreator
    {
        private readonly ILogger<ResponseCreator> logger;

        public ResponseCreator(
            ILogger<ResponseCreator> logger)
        {
            this.logger = CheckValue(logger, nameof(logger));
        }

        public async Task CreateResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            logger.LogDebug("Converting the upstream HTTP response to a downstream response.");

            var downstreamResponse = context.Response;

            downstreamResponse.StatusCode = (int)upstreamResponse.StatusCode;
            await upstreamResponse.Content.CopyToAsync(downstreamResponse.Body);

            // TODO: Headers, Cookies, and Trailing headers. How about content length/types/etc?
        }
    }
}
