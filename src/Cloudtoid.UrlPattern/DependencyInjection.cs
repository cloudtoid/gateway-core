namespace Cloudtoid.UrlPattern
{
    using Microsoft.Extensions.DependencyInjection;
    using static Contract;

    public static class DependencyInjection
    {
        public static IServiceCollection AddUrlPattern(this IServiceCollection services)
        {
            CheckValue(services, nameof(services));

            if (services.Exists<Marker>())
                return services;

            return services
                .TryAddSingleton<Marker>()
                .AddLogging()
                .AddOptions()
                .AddFramework()
                .TryAddSingleton<IUrlPathNormalizer, UrlPathNormalizer>()
                .TryAddSingleton<IPatternParser, PatternParser>()
                .TryAddSingleton<IPatternValidator, PatternValidator>()
                .TryAddSingleton<IPatternCompiler, PatternCompiler>()
                .TryAddSingleton<IPatternMatcher, PatternMatcher>();
        }

        // This class prevents multiple registrations of this library with DI
        private sealed class Marker
        {
        }
    }
}
