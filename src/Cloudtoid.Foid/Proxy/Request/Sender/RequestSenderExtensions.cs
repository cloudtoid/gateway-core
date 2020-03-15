namespace Cloudtoid.Foid.Proxy
{
    using System;
    using System.Net.Http;
    using Cloudtoid.Foid.Options;
    using Microsoft.Extensions.DependencyInjection;

    internal static class RequestSenderExtensions
    {
        internal static IHttpClientBuilder ConfigureDefaultRequestSenderHttpHandler(this IHttpClientBuilder builder)
            => builder.ConfigurePrimaryHttpMessageHandler(ConfigureHttpHandler);

        private static HttpMessageHandler ConfigureHttpHandler(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<OptionsProvider>();
            var senderOptions = options.Proxy.Upstream.Request.Sender;
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = senderOptions.AllowAutoRedirect,
                UseCookies = senderOptions.UseCookies,
            };
        }
    }
}
