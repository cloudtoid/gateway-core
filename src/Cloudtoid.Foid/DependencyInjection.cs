namespace Cloudtoid.Foid
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using static Contract;

    public static class DependencyInjection
    {
        public static IServiceCollection AddFoidProxy(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<ProxyMarker>())
                return services;

            return services
                .TryAddSingleton<ProxyMarker>()
                .AddFramework()
                .AddHttpClient()
                .TryAddSingleton<Proxy.Config>()
                .TryAddSingleton<Proxy.IUriRewriter, Proxy.UriRewriter>()
                .TryAddSingleton<Proxy.IRequestHeaderSetter, Proxy.RequestHeaderSetter>()
                .TryAddSingleton<Proxy.IRequestHeaderValuesProvider, Proxy.RequestHeaderValuesProvider>()
                .TryAddSingleton<Proxy.IRequestSender, Proxy.RequestSender>()
                .TryAddSingleton<Proxy.IRequestCreator, Proxy.RequestCreator>()
                .TryAddSingleton<Proxy.IResponseCreator, Proxy.ResponseCreator>();
        }

        public static IApplicationBuilder UseFoidProxy(this IApplicationBuilder builder)
        {
            CheckValue(builder, nameof(builder));

            var marker = builder.ApplicationServices.GetService<ProxyMarker>();
            if (marker is null)
                throw new InvalidOperationException($"Call {nameof(AddFoidProxy)} before calling {nameof(UseFoidProxy)}");

            return builder.UseMiddleware<Proxy.ProxyMiddleware>();
        }

        private sealed class ProxyMarker
        {
        }
    }
}
