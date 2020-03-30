namespace Cloudtoid.Foid.Settings
{
    using Cloudtoid.Foid.Expression;
    using Cloudtoid.UrlPattern;

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