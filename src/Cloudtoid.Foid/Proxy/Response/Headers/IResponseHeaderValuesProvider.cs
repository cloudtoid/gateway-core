namespace Cloudtoid.Foid.Proxy
{
    using Microsoft.AspNetCore.Http;

    public interface IResponseHeaderValuesProvider
    {
        /// <summary>
        /// By implementing this method, one can change the values of a given header.
        /// Return false, if the header should be omitted.
        /// </summary>
        bool TryGetHeaderValues(HttpContext context, string name, string[] upstreamValues, out string[] downstreamValues);
    }
}
