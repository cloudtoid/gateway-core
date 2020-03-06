namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using static Contract;

    internal sealed class ResponseHeaderValuesProvider : IResponseHeaderValuesProvider
    {
        public bool AllowHeadersWithEmptyValue => false;

        public bool AllowHeadersWithUnderscoreInName => false;

        public bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> upstreamHeaders,
            out IList<string> downstreamHeaders)
        {
            CheckValue(context, nameof(context));
            CheckNonEmpty(name, nameof(name));

            downstreamHeaders = upstreamHeaders;
            return true;
        }
    }
}
