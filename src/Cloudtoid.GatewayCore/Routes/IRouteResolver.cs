using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Cloudtoid.GatewayCore.Routes
{
    internal interface IRouteResolver
    {
        bool TryResolve(
            HttpContext httpContext,
            [NotNullWhen(true)] out Route? route);
    }
}
