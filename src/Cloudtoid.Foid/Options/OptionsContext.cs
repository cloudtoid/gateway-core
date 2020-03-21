namespace Cloudtoid.Foid.Options
{
    using System.Diagnostics.CodeAnalysis;
    using Cloudtoid.Foid.Expression;

    internal sealed class OptionsContext
    {
        private readonly IExpressionEvaluator evaluator;

        internal OptionsContext(
            IExpressionEvaluator evaluator)
        {
            this.evaluator = evaluator;
        }

        [return: NotNullIfNotNull("expression")]
        internal string? Evaluate(ProxyContext context, string? expression)
            => expression is null ? null : evaluator.Evaluate(context, expression);
    }
}
