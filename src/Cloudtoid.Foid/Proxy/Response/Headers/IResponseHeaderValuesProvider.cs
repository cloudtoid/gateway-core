namespace Cloudtoid.Foid.Proxy
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public interface IResponseHeaderValuesProvider
    {
        /// <summary>
        /// By default, headers with an empty value are dropped.
        /// </summary>
        bool AllowHeadersWithEmptyValue { get; }

        /// <summary>
        /// By default, headers with an underscore in their names are dropped.
        /// </summary>
        bool AllowHeadersWithUnderscoreInName { get; }

        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return false, if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(HttpContext context, string name, IList<string> upstreamValues, out IList<string> downstreamValues);

        /// <summary>
        /// Extra headers to be appended to the outgoing downstream response.
        /// </summary>
        IEnumerable<(string Key, string[] Values)> GetExtraHeaders(HttpContext context);
    }
}
