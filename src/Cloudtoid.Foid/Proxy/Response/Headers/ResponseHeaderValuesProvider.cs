namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
    using static Contract;

    internal sealed class ResponseHeaderValuesProvider : IResponseHeaderValuesProvider
    {
        public bool AllowHeadersWithEmptyValue => false;

        public bool AllowHeadersWithUnderscoreInName => false;

        public bool TryGetHeaderValues(
            HttpContext context,
            string name,
            StringValues upstreamHeaders,
            out StringValues downstreamHeaders)
        {
            CheckValue(context, nameof(context));
            CheckNonEmpty(name, nameof(name));

            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
