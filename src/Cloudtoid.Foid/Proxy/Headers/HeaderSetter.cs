namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using static Contract;

    internal sealed class HeaderSetter : IHeaderSetter
    {
        private readonly IHeaderValuesProvider headerValuesProvider;
        private readonly ILogger<HeaderSetter> logger;

        public HeaderSetter(
            IHeaderValuesProvider headerValuesProvider,
            ILogger<HeaderSetter> logger)
        {
            this.headerValuesProvider = CheckValue(headerValuesProvider, nameof(headerValuesProvider));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public Task SetHeadersAsync(HttpContext context, HttpRequestMessage message)
        {
            CheckValue(context, nameof(context));
            CheckValue(message, nameof(message));

            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = context.Request.Headers;
            if (headers is null)
                return Task.CompletedTask;

            foreach (var header in headers)
            {
                // Remove empty headers
                if (!headerValuesProvider.AllowHeadersWithEmptyValue && header.Value.Count == 0)
                {
                    logger.LogDebug("Removing header '{0}' as its value if empty.", header.Key);
                    continue;
                }

                // Remove headers with underscore in their names
                if (!headerValuesProvider.AllowHeadersWithUnderscoreInName && header.Key.Contains('_'))
                {
                    logger.LogDebug("Removing header '{0}' as headers should not have underscores in their names.", header.Key);
                    continue;
                }

                message.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
            }

            if (!headers.ContainsKey(HeaderNames.Host))
                message.Headers.TryAddWithoutValidation(HeaderNames.Host, headerValuesProvider.GetDefaultHostHeaderValue(context));

            return Task.CompletedTask;
        }
    }
}
