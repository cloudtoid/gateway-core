using System.Diagnostics.CodeAnalysis;
using Cloudtoid.GatewayCore.Expression;

namespace Cloudtoid.GatewayCore.Settings
{
    internal sealed class RouteSettingsContext
    {
        private readonly IExpressionEvaluator evaluator;

        internal RouteSettingsContext(
            string route,
            IExpressionEvaluator evaluator)
        {
            Route = route;
            this.evaluator = evaluator;
        }

        public string Route { get; set; }

        [return: NotNullIfNotNull("expression")]
        internal string? Evaluate(ProxyContext context, string? expression)
            => expression is null ? null : evaluator.Evaluate(context, expression);
    }
}
