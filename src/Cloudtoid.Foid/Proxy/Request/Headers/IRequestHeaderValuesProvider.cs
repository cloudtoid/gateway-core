namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public interface IRequestHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return false, if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(
            HttpContext context,
            string name,
            IList<string> downstreamValues,
            out IList<string> upstreamValues);
    }
}
