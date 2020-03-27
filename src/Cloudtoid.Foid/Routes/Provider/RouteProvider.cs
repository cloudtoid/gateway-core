namespace Cloudtoid.Foid.Routes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;

    internal sealed class RouteProvider : IRouteProvider, IReadOnlyCollection<RouteSettings>
    {
        private static readonly object RouteKey = new object();
        private readonly IOptionsMonitor<FoidOptions> options;
        private readonly IRouteSettingsCreator settingsCreator;
        private IReadOnlyList<RouteSettings> routes;

        public RouteProvider(
            IRouteSettingsCreator settingsCreator,
            IOptionsMonitor<FoidOptions> options)
        {
            this.options = CheckValue(options, nameof(options));
            this.settingsCreator = CheckValue(settingsCreator, nameof(settingsCreator));

            options.OnChange(_ => routes = CreateRoutes());
            routes = CreateRoutes();
        }

        public int Count => routes.Count;

        public IEnumerator<RouteSettings> GetEnumerator()
            => routes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => routes.GetEnumerator();

        public bool TryGetRoute(
            HttpContext context,
            [NotNullWhen(true)] out Route? route)
        {
            CheckValue(context, nameof(context));

            // look up in request cache first
            if (context.Items.TryGetValue(RouteKey, out var existing))
            {
                route = (Route)existing;
                return true;
            }

            route = null;
            return false;
        }

        private IReadOnlyList<RouteSettings> CreateRoutes()
        {
            return options.CurrentValue.Routes
                .Select(r => settingsCreator.TryCreate(r.Key, r.Value, out var setting) ? setting : null)
                .WhereNotNull()
                .AsReadOnlyList();
        }
    }
}
