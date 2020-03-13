namespace Cloudtoid.Foid.Expression
{
    using Microsoft.AspNetCore.Http;

    public interface IExpressionEvaluator
    {
        string Evaluate(HttpContext context, string expression);
    }
}
