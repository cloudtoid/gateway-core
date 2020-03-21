namespace Cloudtoid.Foid.Upstream
{
    using System;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;

    internal static class RequestSenderExtensions
    {
        internal static IHttpClientBuilder ConfigureDefaultRequestSenderHttpHandler(this IHttpClientBuilder builder)
            => builder.ConfigurePrimaryHttpMessageHandler(ConfigureHttpHandler);

        private static HttpMessageHandler ConfigureHttpHandler(IServiceProvider serviceProvider)
        {
            // TODO: Commented out until fixed
            // TODO: Commented out until fixed
            // TODO: Commented out until fixed
            // TODO: Commented out until fixed
            // TODO: Commented out until fixed

            //// var options = serviceProvider.GetRequiredService<RouteProvider>();
            //// var senderOptions = options.Proxy.Upstream.Request.Sender;
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false, //// senderOptions.AllowAutoRedirect,
                UseCookies = false //// senderOptions.UseCookies,
            };
        }
    }
}
