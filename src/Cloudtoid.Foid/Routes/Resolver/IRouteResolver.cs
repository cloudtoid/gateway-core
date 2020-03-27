namespace Cloudtoid.Foid.Routes
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Http;

    internal interface IRouteResolver
    {
        bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route route);
    }
}
