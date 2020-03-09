namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;

    public interface IExpressionEvaluator
    {
        string? Evaluate(HttpContext context, string? expression);
    }
}
