namespace Cloudtoid.Foid.UnitTests
{
    using Cloudtoid.Foid.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using NSubstitute;
    using static Contract;

    internal static class DependencyInjection
    {
        public static IServiceCollection AddTest(
            this IServiceCollection services,
            FoidOptions? options = null)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<ProxyMarker>())
                return services;

            if (options != null)
            {
                var monitor = Substitute.For<IOptionsMonitor<FoidOptions>>();
                monitor.CurrentValue.Returns(options);
                services.AddSingleton(monitor);
            }

            return services
                .AddSingleton(GuidProvider.Instance)
                .AddLogging()
                .AddFoidProxy();
        }

        // prevents multiple registrations of this library with DI
        private sealed class ProxyMarker
        {
        }
    }
}
