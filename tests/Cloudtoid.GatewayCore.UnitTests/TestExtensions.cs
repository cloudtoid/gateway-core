using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cloudtoid.GatewayCore.Expression;
using Cloudtoid.GatewayCore.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
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
        public static IServiceCollection AddTest(
            this IServiceCollection services,
            string? gatewayOptionsJsonFile = null,
            bool reloadOnGatewayOptionsJsonFileChange = false,
            GatewayOptions? gatewayOptions = null,
            IConfigurationBuilder? configBuilder = null)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            services
                .AddSingleton(GuidProvider.Instance)
                .Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)))
                .AddGatewayCore();

            if (gatewayOptionsJsonFile is not null)
            {
                configBuilder = new ConfigurationBuilder()
                    .AddJsonFile(gatewayOptionsJsonFile, optional: false, reloadOnChange: reloadOnGatewayOptionsJsonFileChange);
            }

            if (configBuilder is not null)
            {
                services.Configure<GatewayOptions>(configBuilder.Build());
            }
            else
            {
                var monitor = Substitute.For<IOptionsMonitor<GatewayOptions>>();
                monitor.CurrentValue.Returns(gatewayOptions ?? CreateDefaultOptions());
                services.TryAddSingleton(monitor);
            }

            return services;
        }

        public static ProxyContext GetProxyContext(
            this IServiceProvider provider,
            HttpContext? httpContext = null,
            string? pathSuffix = null,
            IReadOnlyDictionary<string, string>? variables = null)
        {
            var settingsProvider = provider.GetRequiredService<ISettingsProvider>();
            var routeOptions = settingsProvider.CurrentValue.Routes[0];

            var route = new Route(
                routeOptions,
                pathSuffix ?? string.Empty,
                variables ?? ImmutableDictionary<string, string>.Empty);

            if (httpContext is null)
            {
                httpContext = new DefaultHttpContext();
                var feature = Substitute.For<IHttpResponseTrailersFeature>();
                feature.Trailers.Returns(new HeaderDictionary());
                httpContext.Features.Set(feature);
            }

            return new ProxyContext(
                provider.GetRequiredService<IExpressionEvaluator>(),
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
