namespace Cloudtoid.GatewayCore.Routes
{
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.GatewayCore.Settings;
    using Cloudtoid.UrlPattern;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RouteResolver : IRouteResolver
    {
        private readonly ISettingsProvider settings;
        private readonly IPatternEngine patternEngine;
        private readonly ILogger<RouteResolver> logger;

        public RouteResolver(
            ISettingsProvider settings,
            IPatternEngine patternEngine,
            ILogger<RouteResolver> logger)
        {
            this.settings = CheckValue(settings, nameof(settings));
            this.patternEngine = CheckValue(patternEngine, nameof(patternEngine));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route? route)
        {
            var path = httpContext.Request.Path;
            foreach (var routeSetting in settings.CurrentValue.Routes)
            {
                if (patternEngine.TryMatch(routeSetting.CompiledRoute, path, out var match, out var why))
                {
                    route = new Route(routeSetting, match.PathSuffix, match.Variables);
                    return true;
                }

                // Keep this as info because any genuine match also returns the reason.
                logger.LogInformation(why);
            }

            route = null;
            return false;
        }
    }
}
