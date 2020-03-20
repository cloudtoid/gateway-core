namespace Cloudtoid.Foid.Routes
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;

    public interface IRouteProvider : IReadOnlyCollection<RouteOptions>
    {
        bool TryGetRoute(
           HttpContext context,
           [MaybeNullWhen(false)] out Route route);
    }
}
