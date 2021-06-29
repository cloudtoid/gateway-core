using System.Diagnostics.CodeAnalysis;
using Cloudtoid.GatewayCore.Settings;
using Cloudtoid.UrlPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Routes
{
    internal sealed class RouteResolver : IRouteResolver
    {
        private static readonly MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions { Size = 1 };
        private readonly ISettingsProvider settings;
        private readonly IPatternEngine patternEngine;
        private readonly ILogger<RouteResolver> logger;
        private readonly IMemoryCache cache;

        public RouteResolver(
            ISettingsProvider settings,
            IPatternEngine patternEngine,
            ILogger<RouteResolver> logger)
        {
            this.settings = CheckValue(settings, nameof(settings));
            this.patternEngine = CheckValue(patternEngine, nameof(patternEngine));
            this.logger = CheckValue(logger, nameof(logger));

            var cacheOptions = new MemoryCacheOptions
            {
                SizeLimit = settings.CurrentValue.System.RouteCacheMaxCount // maximum number of cached entries
            };

            cache = new MemoryCache(cacheOptions);
        }

        public bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route? route)
        {
            var path = httpContext.Request.Path;

            if (cache.TryGetValue(path, out route) && route is not null)
                return true;

            foreach (var routeSetting in settings.CurrentValue.Routes)
            {
                if (patternEngine.TryMatch(routeSetting.CompiledRoute, path, out var match, out var why))
                {
                    route = new Route(routeSetting, match.PathSuffix, match.Variables);
                    cache.Set(path, route, CacheEntryOptions);
                    return true;
                }

                // Keep this as info because any genuine no-match also returns the reason.
                logger.LogInformation(why);
            }

            route = null;
            return false;
        }
    }
}
