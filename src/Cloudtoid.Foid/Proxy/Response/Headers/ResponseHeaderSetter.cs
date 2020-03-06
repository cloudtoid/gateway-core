namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Net.Http.Headers;
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
                if (!headerValuesProvider.AllowHeadersWithEmptyValue && !header.Value.Any())
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

                responseHeaders[header.Key] = header.Value.AsArray();
            }

            return Task.CompletedTask;
        }
    }
}
