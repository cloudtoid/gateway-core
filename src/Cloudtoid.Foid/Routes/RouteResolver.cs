namespace Cloudtoid.Foid.Routes
{
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.Foid.Settings;
    using Cloudtoid.UrlPattern;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class RouteResolver : IRouteResolver
    {
        private readonly ISettingsProvider settings;
        private readonly IPatternMatcher matcher;

        public RouteResolver(
            ISettingsProvider settings,
            IPatternMatcher matcher)
        {
            this.settings = CheckValue(settings, nameof(settings));
            this.matcher = CheckValue(matcher, nameof(matcher));
        }

        public bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route? route)
        {
            var path = httpContext.Request.Path;
            var routes = settings.CurrentValue.Routes;
            foreach (var routeSetting in routes)
            {
                if (matcher.TryMatch(routeSetting.CompiledRoute, path, out var match))
                {
                    route = new Route(routeSetting, match.Variables);
                    return true;
                }
            }

            route = null;
            return false;
        }
    }
}
