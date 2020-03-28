namespace Cloudtoid.Foid.Settings
{
    using Cloudtoid.UrlPattern;

    public sealed class RouteSettings
    {
        internal RouteSettings(
            string route,
            CompiledPattern compiledRoute,
            ProxySettings? proxySettings)
        {
            Route = route;
            CompiledRoute = compiledRoute;
            Proxy = proxySettings;
        }

        public string Route { get; }

        public CompiledPattern CompiledRoute { get; }

        public ProxySettings? Proxy { get; }
    }
}