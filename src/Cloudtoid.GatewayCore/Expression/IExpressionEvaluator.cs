namespace Cloudtoid.GatewayCore.Expression
{
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// Evaluate an expression and returns the result in the form of a string.
        /// This method returns <paramref name="expression"/> if there is nothing to evaluate.
        /// </summary>
        string Evaluate(ProxyContext context, string expression);
    }
}
