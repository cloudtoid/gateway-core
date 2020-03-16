namespace Cloudtoid.Foid
{
    using System;
    using System.Net.Http;
    using Cloudtoid.Foid.Proxy;
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
        public static IProxyBuilder AddFoidProxy(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return new ProxyBuilder(services);

            var httpClientBuilder = services
                .AddFoidProxyCore(DefaultRequestSenderHttpClientName)
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
        /// services.AddFoidProxy("MyHttpClient");
        /// </c>
        /// </example>
        public static IServiceCollection AddFoidProxy(
            this IServiceCollection services,
            string requestSenderHttpClientName)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            return services.AddFoidProxyCore(requestSenderHttpClientName);
        }

        public static IApplicationBuilder UseFoidProxy(this IApplicationBuilder builder)
        {
            CheckValue(builder, nameof(builder));

            var marker = builder.ApplicationServices.GetService<Marker>();
            if (marker is null)
                throw new InvalidOperationException($"Call {nameof(AddFoidProxy)} before calling {nameof(UseFoidProxy)}");

            return builder.UseMiddleware<ProxyMiddleware>();
        }

        private static IServiceCollection AddFoidProxyCore(
            this IServiceCollection services,
            string requestSenderHttpClientName)
        {
            CheckValue(services, nameof(services));

            return services
                .TryAddSingleton<Marker>()
                .AddFramework()
                .AddOptions()
                .TryAddSingleton<Options.OptionsProvider>()
                .TryAddSingleton<Trace.ITraceIdProvider, Trace.TraceIdProvider>()
                .TryAddSingleton<Host.IHostProvider, Host.HostProvider>()
                .TryAddSingleton<Expression.IExpressionEvaluator, Expression.ExpressionEvaluator>()
                .TryAddSingleton<IUriRewriter, UriRewriter>()
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
                .TryAddSingleton<IResponseContentHeaderValuesProvider, ResponseContentHeaderValuesProvider>();
        }

        // This class prevents multiple registrations of this library with DI
        private sealed class Marker
        {
        }
    }
}
