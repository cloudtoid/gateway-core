using System.Collections.Generic;

namespace Cloudtoid.GatewayCore.Settings
{
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
