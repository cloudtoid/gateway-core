namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    public class ResponseHeaderValuesProvider : IResponseHeaderValuesProvider
    {
        public virtual bool TryGetHeaderValues(
            HttpContext context,
            string name,
            string[] upstreamHeaders,
            out string[] downstreamHeaders)
        {
            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
