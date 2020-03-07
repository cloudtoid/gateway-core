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
        private readonly IResponseHeaderValuesProvider provider;
        private readonly ILogger<ResponseHeaderSetter> logger;

        public ResponseHeaderSetter(
            IResponseHeaderValuesProvider provider,
            ILogger<ResponseHeaderSetter> logger)
        {
            this.provider = CheckValue(provider, nameof(provider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            CheckValue(context, nameof(context));
            CheckValue(upstreamResponse, nameof(upstreamResponse));

            context.RequestAborted.ThrowIfCancellationRequested();

            AddUpstreamHeadersToDownstream(context, upstreamResponse);

            return Task.CompletedTask;
        }

        private void AddUpstreamHeadersToDownstream(HttpContext context, HttpResponseMessage upstreamResponse)
        {
            if (provider.IgnoreAllUpstreamResponseHeaders)
                return;

            var headers = upstreamResponse.Headers;
            if (headers is null)
                return;

            foreach (var header in headers)
            {
                var key = header.Key;

                // Remove empty headers
                if (!provider.AllowHeadersWithEmptyValue && header.Value.All(s => string.IsNullOrEmpty(s)))
                {
                    logger.LogInformation("Removing header '{0}' as its value is empty.", key);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!provider.AllowHeadersWithUnderscoreInName && key.Contains('_'))
                {
                    logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", header.Key);
                    continue;
                }

                AddHeaderValues(context, upstreamResponse, key, header.Value.AsArray());
            }
        }

        private void AddHeaderValues(
            HttpContext context,
            HttpResponseMessage upstreamResponse,
            string key,
            params string[] upstreamValues)
        {
            if (provider.TryGetHeaderValues(context, key, upstreamValues, out var downstreamValues) && downstreamValues != null)
            {
                context.Response.Headers[key] = downstreamValues;
                return;
            }

            logger.LogInformation(
                "Header '{0}' is not added. This was was instructed by the {1}.{2}.",
                key,
                nameof(IResponseHeaderValuesProvider),
                nameof(IResponseHeaderValuesProvider.TryGetHeaderValues));
        }
    }
}
