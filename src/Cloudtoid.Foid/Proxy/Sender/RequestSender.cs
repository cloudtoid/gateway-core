namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class RequestSender : IRequestSender
    {
        private readonly IHttpClientFactory httpClientFactory;

        public RequestSender(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = CheckValue(httpClientFactory, nameof(httpClientFactory));
        }

        public async Task<HttpResponseMessage> SendAsync(HttpContext context, CancellationToken cancellationToken)
        {
            CheckValue(context, nameof(context));

            context.RequestAborted.ThrowIfCancellationRequested();

            var client = httpClientFactory.CreateClient();
            return await client.SendAsync(null, cancellationToken);
        }
    }
}
