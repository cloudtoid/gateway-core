namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can be fully in control of setting the outgoing downstream response headers.
    /// First consider implementing <see cref="IResponseHeaderValuesProvider"./>. If that does not meet your needs then implement <see cref="IResponseHeaderSetter"/>.
    /// </summary>
    public interface IResponseHeaderSetter
    {
        Task SetHeadersAsync(HttpContext context, HttpResponseMessage upstreamResponse);
    }
}
