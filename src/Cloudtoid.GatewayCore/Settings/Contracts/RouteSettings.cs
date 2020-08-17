using Cloudtoid.GatewayCore.Expression;
using Cloudtoid.UrlPattern;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class RouteSettings
    {
        internal RouteSettings(
            string route,
            CompiledPattern compiledRoute,
            VariableTrie<string> variableTrie,
            ProxySettings? proxySettings)
        {
            Route = route;
            CompiledRoute = compiledRoute;
            VariableTrie = variableTrie;
            Proxy = proxySettings;
        }

        public string Route { get; }

        public CompiledPattern CompiledRoute { get; }

        public ProxySettings? Proxy { get; }

        internal VariableTrie<string> VariableTrie { get; }
    }
}