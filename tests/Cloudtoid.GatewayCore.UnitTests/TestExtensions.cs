using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cloudtoid.GatewayCore.Settings;
using Cloudtoid.GatewayCore.Trace;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.UnitTests
{
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
                .AddGatewayCore();

            return services;
        }

        public static IServiceCollection AddTestOptions(
            this IServiceCollection services,
            GatewayOptions? options = null)
        {
            options ??= CreateDefaultOptions();
            var monitor = Substitute.For<IOptionsMonitor<GatewayOptions>>();
            monitor.CurrentValue.Returns(options);
            services.TryAddSingleton(monitor);
            return services;
        }

        public static ProxyContext GetProxyContext(
            this IServiceProvider provider,
            HttpContext? httpContext = null,
            string? pathSuffix = null,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            var settingsProvider = provider.GetRequiredService<ISettingsProvider>();
            var routeOptions = settingsProvider.CurrentValue.Routes.First();

            httpContext ??= new DefaultHttpContext();
            var route = new Route(
                routeOptions,
                pathSuffix ?? string.Empty,
                variables ?? ImmutableDictionary<string, string>.Empty);

            return new ProxyContext(
                provider.GetRequiredService<ITraceIdProvider>(),
                httpContext,
                route);
        }

        public static GatewayOptions CreateDefaultOptions(
            string route = "/api/",
            string to = "/upstream/api/")
        {
            return new GatewayOptions
            {
                Routes = new Dictionary<string, GatewayOptions.RouteOptions>
                {
                    [route] = new GatewayOptions.RouteOptions
                    {
                        Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                        {
                            To = to
                        }
                    },
                    [route + "v2/"] = new GatewayOptions.RouteOptions
                    {
                        Proxy = new GatewayOptions.RouteOptions.ProxyOptions
                        {
                            To = to + "v2/"
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
