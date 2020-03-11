namespace Cloudtoid.Foid.Proxy
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// By implementing this interface, one can be fully in control of setting the outbound upstream request headers.
    /// First consider implementing <see cref="IRequestHeaderValuesProvider"./>. If that does not meet your needs then implement <see cref="IRequestHeaderSetter"/>.
    /// </summary>
    public interface IRequestHeaderSetter
    {
        Task SetHeadersAsync(
            HttpContext context,
            HttpRequestMessage upstreamRequest);
    }
}
