namespace Cloudtoid.Foid.Routes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cloudtoid.Foid.Expression;
    using Cloudtoid.Foid.Options;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using static Contract;
    using Options = Options.FoidOptions;

    internal sealed class RouteProvider : IRouteProvider, IReadOnlyCollection<RouteOptions>
    {
        private static readonly object RouteKey = new object();
        private readonly IOptionsMonitor<Options> options;
        private readonly OptionsContext context;
        private IReadOnlyList<RouteOptions> routes;

        public RouteProvider(
            IExpressionEvaluator evaluator,
            IOptionsMonitor<Options> options)
        {
            this.options = CheckValue(options, nameof(options));
            context = new OptionsContext(CheckValue(evaluator, nameof(evaluator)));

            options.OnChange(_ => routes = CreateRoutes());
            routes = CreateRoutes();
        }

        public int Count => routes.Count;

        public IEnumerator<RouteOptions> GetEnumerator() => routes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => routes.GetEnumerator();

        public bool TryGetRoute(
            HttpContext context,
            [MaybeNullWhen(false)] out Route route)
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

        private IReadOnlyList<RouteOptions> CreateRoutes()
        {
            return options.CurrentValue.Routes
                .Select(r => new RouteOptions(context, r.Key, r.Value))
                .AsReadOnlyList();
        }
    }
}
