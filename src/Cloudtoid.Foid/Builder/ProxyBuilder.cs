namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class ProxyBuilder : IProxyBuilder
    {
        internal ProxyBuilder(
            IServiceCollection services,
            IHttpClientBuilder? requestSenderHttpClientBuilder = null)
        {
            Services = services;
            RequestSenderHttpClientBuilder = requestSenderHttpClientBuilder;
        }

        public IServiceCollection Services { get; }

#pragma warning disable CS8613
        public IHttpClientBuilder? RequestSenderHttpClientBuilder { get; }
#pragma warning restore CS8613
    }
}
