namespace Cloudtoid.Foid.Settings
{
    public sealed class RouteSettings
    {
        internal RouteSettings(
            string route,
            ProxySettings? proxySettings)
        {
            Route = route;
            Proxy = proxySettings;
        }

        public string Route { get; }

        public ProxySettings? Proxy { get; }
    }
}