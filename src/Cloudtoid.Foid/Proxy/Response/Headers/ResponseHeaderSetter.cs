namespace Cloudtoid.Foid.Proxy
{
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class ResponseHeaderSetter : IResponseHeaderSetter
    {
        private readonly IResponseHeaderValuesProvider headerValuesProvider;
        private readonly ILogger<ResponseHeaderSetter> logger;

        public ResponseHeaderSetter(
            IResponseHeaderValuesProvider headerValuesProvider,
            ILogger<ResponseHeaderSetter> logger)
        {
            this.headerValuesProvider = CheckValue(headerValuesProvider, nameof(headerValuesProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            var responseHeaders = context.Response.Headers;
            foreach (var header in upstreamResponse.Headers)
            {
                // Remove empty headers
                if (!headerValuesProvider.AllowHeadersWithEmptyValue && header.Value.All(s => string.IsNullOrEmpty(s)))
                {
                    logger.LogInformation("Removing header '{0}' as its value is empty.", header.Key);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!headerValuesProvider.AllowHeadersWithUnderscoreInName && header.Key.Contains('_'))
                {
                    logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", header.Key);
                    continue;
                }

                if (!headerValuesProvider.TryGetHeaderValues(context, header.Key, header.Value.AsList(), out var downstreamValues) || downstreamValues is null)
                {
                    logger.LogInformation(
                        "Removing header '{0}' as was instructed by the {1}.",
                        header.Key,
                        nameof(IResponseHeaderValuesProvider));

                    continue;
                }

                responseHeaders[header.Key] = downstreamValues.AsArray();
            }

            return Task.CompletedTask;
        }
    }
}
