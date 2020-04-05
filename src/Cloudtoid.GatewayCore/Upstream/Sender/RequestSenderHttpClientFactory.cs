namespace Cloudtoid.GatewayCore.Upstream
{
    using System;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using static Contract;

    internal sealed class RequestSenderHttpClientFactory : IRequestSenderHttpClientFactory
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string httpClientName;

        internal RequestSenderHttpClientFactory(IServiceProvider serviceProvider, string httpClientName)
        {
            CheckValue(serviceProvider, nameof(serviceProvider));

            httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            this.httpClientName = httpClientName;
        }

        public HttpClient CreateClient() => httpClientFactory.CreateClient(httpClientName);
    }
}
