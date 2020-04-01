namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Settings;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NSubstitute;
    using static Contract;

    internal static class TestExtensions
    {
        public static IServiceCollection AddTest(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            services
                .AddSingleton(GuidProvider.Instance)
                .Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)))
                .AddFoidProxy();

            return services;
        }

        public static IServiceCollection AddTestOptions(
            this IServiceCollection services,
            ReverseProxyOptions? options = null)
        {
            options ??= CreateDefaultOptions();
            var monitor = Substitute.For<IOptionsMonitor<ReverseProxyOptions>>();
            monitor.CurrentValue.Returns(options);
            services.TryAddSingleton(monitor);
            return services;
        }

        public static ProxyContext GetProxyContext(
            this IServiceProvider provider,
            HttpContext? httpContext = null,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            var settingsProvider = provider.GetRequiredService<ISettingsProvider>();
            var routeOptions = settingsProvider.CurrentValue.Routes.First();

            httpContext ??= new DefaultHttpContext();

            return new ProxyContext(
                provider.GetRequiredService<IHostProvider>(),
                provider.GetRequiredService<ITraceIdProvider>(),
                httpContext,
                new Route(routeOptions, string.Empty, variables ?? ImmutableDictionary<string, string>.Empty));
        }

        public static ReverseProxyOptions CreateDefaultOptions(string route = "/api/", string to = "/upstream/api/")
        {
            return new ReverseProxyOptions
            {
                Routes = new Dictionary<string, ReverseProxyOptions.RouteOptions>
                {
                    [route] = new ReverseProxyOptions.RouteOptions
                    {
                        Proxy = new ReverseProxyOptions.RouteOptions.ProxyOptions
                        {
                            To = to
                        }
                    }
                }
            };
        }

        // prevents multiple registrations of this library with DI
        private sealed class Marker
        {
        }
    }
}
