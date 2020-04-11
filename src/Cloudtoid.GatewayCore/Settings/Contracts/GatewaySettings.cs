namespace Cloudtoid.GatewayCore.Settings
{
    using System.Collections.Generic;

    public sealed class GatewaySettings
    {
        internal GatewaySettings(SystemSettings system, IReadOnlyList<RouteSettings> routes)
        {
            System = system;
            Routes = routes;
        }

        public SystemSettings System { get; }

        public IReadOnlyList<RouteSettings> Routes { get; }
    }
}
