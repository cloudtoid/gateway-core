namespace Cloudtoid.Foid.Expression
{
    public interface IExpressionEvaluator
    {
        string Evaluate(ProxyContext context, string expression);
    }
}
