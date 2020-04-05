namespace Cloudtoid.GatewayCore.Settings
{
    using System.Collections.Generic;

    public sealed class ReverseProxySettings
    {
        internal ReverseProxySettings(IReadOnlyList<RouteSettings> routes)
        {
            Routes = routes;
        }

        public IReadOnlyList<RouteSettings> Routes { get; }
    }
}
