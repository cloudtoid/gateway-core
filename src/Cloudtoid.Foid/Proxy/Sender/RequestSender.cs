namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using static Contract;

    internal sealed class RequestSender : IRequestSender
    {
        private readonly IHttpClientFactory httpClientFactory;

        public RequestSender(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = CheckValue(httpClientFactory, nameof(httpClientFactory));
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage upstreamMessage, CancellationToken cancellationToken)
        {
            CheckValue(upstreamMessage, nameof(upstreamMessage));

            cancellationToken.ThrowIfCancellationRequested();

            // TODO:
            // 1- Need timeout
            // 2- Need to log errors and so on
            // 3- Need to log the steps EVERYWHERE as Info/Debug

            var client = httpClientFactory.CreateClient();
            return await client.SendAsync(upstreamMessage, cancellationToken);
        }
    }
}
