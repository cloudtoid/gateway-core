namespace Cloudtoid.GatewayCore.Upstream
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

            ////var settings = serviceProvider.GetRequiredService<ISettingsProvider>();
            ////var senderOptions = settings.CurrentValue..Proxy.Upstream.Request.Sender;
            return new SocketsHttpHandler
            {
                AllowAutoRedirect = false, //// senderOptions.AllowAutoRedirect,
                UseCookies = false //// senderOptions.UseCookies,
            };
        }
    }
}
