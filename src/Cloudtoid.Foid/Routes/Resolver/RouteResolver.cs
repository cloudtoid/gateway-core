namespace Cloudtoid.Foid.Routes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Http;

    internal sealed class RouteResolver : IRouteResolver
    {
        public bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route route)
        {
            throw new NotImplementedException();
        }
    }
}
