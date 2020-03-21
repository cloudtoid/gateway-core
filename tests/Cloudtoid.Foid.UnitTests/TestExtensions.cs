namespace Cloudtoid.Foid.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cloudtoid.Foid.Host;
    using Cloudtoid.Foid.Options;
    using Cloudtoid.Foid.Routes;
    using Cloudtoid.Foid.Trace;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
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
                .AddLogging()
                .AddFoidProxy();

            return services;
        }

        public static IServiceCollection AddTestOptions(
            this IServiceCollection services,
            FoidOptions? options = null)
        {
            options ??= CreateDefaultOptions();
            var monitor = Substitute.For<IOptionsMonitor<FoidOptions>>();
            monitor.CurrentValue.Returns(options);
            services.TryAddSingleton(monitor);
            return services;
        }

        public static CallContext GetCallContext(
            this IServiceProvider provider,
            HttpContext? httpContext = null)
        {
            var routeProvider = provider.GetRequiredService<IRouteProvider>();
            var routeOptions = routeProvider.First();

            httpContext ??= new DefaultHttpContext();

            return new CallContext(
                provider.GetRequiredService<IHostProvider>(),
                provider.GetRequiredService<ITraceIdProvider>(),
                httpContext,
                new Route(routeOptions));
        }

        public static FoidOptions CreateDefaultOptions()
        {
            return new FoidOptions
            {
                Routes = new Dictionary<string, FoidOptions.RouteOptions>
                {
                    ["/api/"] = new FoidOptions.RouteOptions
                    {
                        Proxy = new FoidOptions.RouteOptions.ProxyOptions
                        {
                            To = "/upstream/api/"
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
