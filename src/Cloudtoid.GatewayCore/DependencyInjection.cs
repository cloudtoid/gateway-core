using System;
using Cloudtoid.GatewayCore.Downstream;
using Cloudtoid.GatewayCore.Proxy;
using Cloudtoid.GatewayCore.Upstream;
using Cloudtoid.UrlPattern;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Gateway Core services to the <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddGatewayCore(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            services
                .AddGatewayCoreInternal()
                .AddHttpClient();

            return services;
        }

        public static IApplicationBuilder UseGatewayCore(this IApplicationBuilder builder)
        {
            CheckValue(builder, nameof(builder));

            var serviceProvider = builder.ApplicationServices;
            var marker = serviceProvider.GetService<Marker>();
            if (marker is null)
                throw new InvalidOperationException($"Call {nameof(AddGatewayCore)} before calling {nameof(UseGatewayCore)}");

            return builder
                .UseMiddleware<ProxyExceptionHandlerMiddleware>()
                .UseMiddleware<ProxyMiddleware>();
        }

        private static IServiceCollection AddGatewayCoreInternal(this IServiceCollection services)
        {
            return services
                .TryAddSingleton<Marker>()
                .AddOptions()
                .AddLogging()
                .AddFramework()
                .AddUrlPattern()
                .TryAddSingleton<Settings.ISettingsCreator, Settings.SettingsCreator>()
                .TryAddSingleton<Settings.ISettingsProvider, Settings.SettingsProvider>()
                .TryAddSingleton<Expression.IExpressionEvaluator, Expression.ExpressionEvaluator>()
                .TryAddSingleton<Routes.IRouteResolver, Routes.RouteResolver>()
                .TryAddSingleton<IUpstreamUrlCreator, UpstreamUrlCreator>()
                .TryAddSingleton<IRequestHeaderSetter, RequestHeaderSetter>()
                .TryAddSingleton<IRequestHeaderValuesProvider, RequestHeaderValuesProvider>()
                .TryAddSingleton<IRequestContentSetter, RequestContentSetter>()
                .TryAddSingleton<IRequestSender, RequestSender>()
                .TryAddSingleton<IRequestCreator, RequestCreator>()
                .TryAddSingleton<IResponseSender, ResponseSender>()
                .TryAddSingleton<IResponseHeaderSetter, ResponseHeaderSetter>()
                .TryAddSingleton<IResponseHeaderValuesProvider, ResponseHeaderValuesProvider>()
                .TryAddSingleton<IResponseContentSetter, ResponseContentSetter>()
                .TryAddSingleton<IResponseContentHeaderValuesProvider, ResponseContentHeaderValuesProvider>()
                .TryAddSingleton<ITrailingHeaderSetter, TrailingHeaderSetter>()
                .TryAddSingleton<ITrailingHeaderValuesProvider, TrailingHeaderValuesProvider>();
        }

        // This class prevents multiple registrations of this library with DI
        private sealed class Marker
        {
        }
    }
}
