namespace Cloudtoid.Foid.Expression
{
    public interface IExpressionEvaluator
    {
        string Evaluate(CallContext context, string expression);
    }
}
