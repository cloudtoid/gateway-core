namespace Cloudtoid.GatewayCore
{
    using System;
    using System.Net.Http;
    using Cloudtoid.GatewayCore.Downstream;
    using Cloudtoid.GatewayCore.Proxy;
    using Cloudtoid.GatewayCore.Upstream;
    using Cloudtoid.UrlPattern;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using static Contract;

    public static class DependencyInjection
    {
        private const string DefaultRequestSenderHttpClientName = "upstream-request-client";

        /// <summary>
        /// Adds the proxy services to <see cref="IServiceCollection"/> and configures a default <see cref="HttpClient"/>
        /// which is used by <see cref="IRequestSender"/> to send upstream HTTP request.
        /// </summary>
        public static IProxyBuilder AddGatewayCore(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return new ProxyBuilder(services);

            var httpClientBuilder = services
                .AddGatewayCoreCore(DefaultRequestSenderHttpClientName)
                .AddHttpClient(DefaultRequestSenderHttpClientName)
                .ConfigureDefaultRequestSenderHttpHandler();

            return new ProxyBuilder(
                services,
                httpClientBuilder);
        }

        /// <summary>
        /// Adds the proxy services to <see cref="IServiceCollection"/>.
        /// The <see cref="HttpClient"/> used by <see cref="IRequestSender"/> to send upstream HTTP requests
        /// expects to find a registered <see cref="HttpClient"/> with <paramref name="requestSenderHttpClientName"/> name.
        /// </summary>
        /// <example>
        /// <c>
        /// services.AddHttpClient("MyHttpClient);
        ///
        /// services.AddGatewayCore("MyHttpClient");
        /// </c>
        /// </example>
        public static IServiceCollection AddGatewayCore(
            this IServiceCollection services,
            string requestSenderHttpClientName)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            return services.AddGatewayCoreCore(requestSenderHttpClientName);
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

        private static IServiceCollection AddGatewayCoreCore(
            this IServiceCollection services,
            string requestSenderHttpClientName)
        {
            CheckValue(services, nameof(services));

            return services
                .TryAddSingleton<Marker>()
                .AddOptions()
                .AddFramework()
                .AddUrlPattern()
                .TryAddSingleton<Settings.ISettingsCreator, Settings.SettingsCreator>()
                .TryAddSingleton<Settings.ISettingsProvider, Settings.SettingsProvider>()
                .TryAddSingleton<Trace.ITraceIdProvider, Trace.TraceIdProvider>()
                .TryAddSingleton<Host.IHostProvider, Host.HostProvider>()
                .TryAddSingleton<Expression.IExpressionEvaluator, Expression.ExpressionEvaluator>()
                .TryAddSingleton<Routes.IRouteResolver, Routes.RouteResolver>()
                .TryAddSingleton<IUpstreamUrlCreator, UpstreamUrlCreator>()
                .TryAddSingleton<IRequestHeaderSetter, RequestHeaderSetter>()
                .TryAddSingleton<IRequestHeaderValuesProvider, RequestHeaderValuesProvider>()
                .TryAddSingleton<IRequestContentSetter, RequestContentSetter>()
                .TryAddSingleton<IRequestContentHeaderValuesProvider, RequestContentHeaderValuesProvider>()
                .TryAddSingleton<IRequestSenderHttpClientFactory>(sp => new RequestSenderHttpClientFactory(sp, requestSenderHttpClientName))
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
