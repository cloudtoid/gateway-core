namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    internal sealed class RequestHeaderSetter : IRequestHeaderSetter
    {
        private readonly IRequestHeaderValuesProvider headerValuesProvider;
        private readonly ILogger<RequestHeaderSetter> logger;

        public RequestHeaderSetter(
            IRequestHeaderValuesProvider headerValuesProvider,
            ILogger<RequestHeaderSetter> logger)
        {
            this.headerValuesProvider = CheckValue(headerValuesProvider, nameof(headerValuesProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task SetHeadersAsync(HttpContext context, HttpRequestMessage upstreamRequest)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamRequest, nameof(upstreamRequest));

            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = context.Request.Headers;
            if (headers is null)
                return Task.CompletedTask;

            foreach (var header in headers)
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

                if (!headerValuesProvider.TryGetHeaderValues(context, header.Key, header.Value, out var upstreamHeaderValues))
                {
                    logger.LogInformation(
                        "Removing header '{0}' as was instructed by the {1}.",
                        header.Key,
                        nameof(IRequestHeaderValuesProvider));

                    continue;
                }

                upstreamRequest.Headers.TryAddWithoutValidation(
                    header.Key,
                    (IEnumerable<string>)upstreamHeaderValues);
            }

            if (!headers.ContainsKey(HeaderNames.Host))
            {
                upstreamRequest.Headers.TryAddWithoutValidation(
                    HeaderNames.Host,
                    headerValuesProvider.GetDefaultHostHeaderValue(context));
            }

            return Task.CompletedTask;
        }
    }
}
