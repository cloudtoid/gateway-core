namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class HeaderSetter : IHeaderSetter
    {
        public Task SetHeadersAsync(HttpContext context, HttpRequestMessage message)
        {
            CheckValue(context, nameof(context));
            CheckValue(message, nameof(message));

            context.RequestAborted.ThrowIfCancellationRequested();

            var headers = context.Request.Headers;
            if (headers is null)
                return Task.CompletedTask;

            // TODO: Read about reverse proxy and what headers need to be copied and what headers should be added
            // also, should we remove headers with empty string as their value?

            foreach (var header in headers)
                message.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);

            return Task.CompletedTask;
        }
    }
}
