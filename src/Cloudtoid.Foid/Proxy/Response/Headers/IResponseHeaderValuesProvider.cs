namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can have more control over the outbound downstream response headers.
    /// </summary>
    public interface IResponseHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return <c>false</c> if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(HttpContext context, string name, string[] upstreamValues, out string[] downstreamValues);
    }
}
