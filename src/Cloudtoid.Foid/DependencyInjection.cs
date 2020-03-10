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
                .AddOptions()
                .AddHttpClient()
                .TryAddSingleton<OptionsProvider>()
                .TryAddSingleton<ITraceIdProvider, TraceIdProvider>()
                .TryAddSingleton<IHostProvider, HostProvider>()
                .TryAddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
                .TryAddSingleton<Proxy.IUriRewriter, Proxy.UriRewriter>()
                .TryAddSingleton<Proxy.IRequestHeaderSetter, Proxy.RequestHeaderSetter>()
                .TryAddSingleton<Proxy.IRequestHeaderValuesProvider, Proxy.RequestHeaderValuesProvider>()
                .TryAddSingleton<Proxy.IRequestSender, Proxy.RequestSender>()
                .TryAddSingleton<Proxy.IRequestCreator, Proxy.RequestCreator>()
                .TryAddSingleton<Proxy.IResponseCreator, Proxy.ResponseCreator>()
                .TryAddSingleton<Proxy.IResponseHeaderSetter, Proxy.ResponseHeaderSetter>()
                .TryAddSingleton<Proxy.IResponseHeaderValuesProvider, Proxy.ResponseHeaderValuesProvider>();
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
