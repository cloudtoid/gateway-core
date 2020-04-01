namespace Cloudtoid.Foid.Routes
{
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.Foid.Settings;
    using Cloudtoid.UrlPattern;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class RouteResolver : IRouteResolver
    {
        private readonly ISettingsProvider settings;
        private readonly IPatternMatcher matcher;
        private readonly IUrlPathNormalizer normalizer;
        private readonly ILogger<RouteResolver> logger;

        public RouteResolver(
            ISettingsProvider settings,
            IPatternMatcher matcher,
            IUrlPathNormalizer normalizer,
            ILogger<RouteResolver> logger)
        {
            this.settings = CheckValue(settings, nameof(settings));
            this.matcher = CheckValue(matcher, nameof(matcher));
            this.normalizer = CheckValue(normalizer, nameof(normalizer));
            this.logger = CheckValue(logger, nameof(logger));
        }

        public bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route? route)
        {
            var path = normalizer.Normalize(httpContext.Request.Path);
            foreach (var routeSetting in settings.CurrentValue.Routes)
            {
                if (matcher.TryMatch(routeSetting.CompiledRoute, path, out var match, out var why))
                {
                    route = new Route(routeSetting, PathString.FromUriComponent(match.PathSuffix), match.Variables);
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
