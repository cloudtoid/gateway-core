namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;

    public interface ITraceIdProvider
    {
        string GetCorrelationId(HttpContext context);

        string GetCallId(HttpContext context);
    }
}
