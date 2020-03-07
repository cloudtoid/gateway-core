namespace Cloudtoid.Foid
{
    using Microsoft.AspNetCore.Http;

    public interface ITraceIdProvider
    {
        string GetRequestId(HttpContext context);

        string GetCallId(HttpContext context);
    }
}
